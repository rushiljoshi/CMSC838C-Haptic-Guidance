using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using UnityEngine;
using TMPro;


public class rotateIMU : MonoBehaviour
{

    public float headoffset = 1.728f;
    public GameObject humanoid;
    public GameObject ovr;

    public GameObject targetHaptics;
    public GameObject controlHaptics;

    private GameObject targetArmCenter;
    private GameObject targetWristCenter;
    private GameObject controlArmCenter;
    private GameObject controlWristCenter;
    private bool targetSet = false;
    private float maxDistWrist = 0.0f;
    private float maxDistArm = 0.0f;
    private float timer = 0.0f;

    public Transform humerus;
    public Transform radius;

    public GameObject humRef;
    public GameObject radRef;

    public GameObject centerEyeAnchor;

    private TcpClient socketConnection; 	
	private Thread clientReceiveThread; 

    private volatile bool alive = false;

    private int[] haptics = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

    private Vector3 rotRad;
    private Vector3 rotHum;

    private Vector3 radOffset = new Vector3(0.0f, 180, 0.0f);
    private Vector3 humOffset = new Vector3(0.0f, 180, 0.0f);
    public Vector3 humCoef = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 radCoef = new Vector3(1.0f, 1.0f, 1.0f);


    void Start () {
        alive=false;

        

        targetWristCenter = targetHaptics.transform.Find("WristCenter").gameObject;
        targetArmCenter = targetHaptics.transform.Find("ArmCenter").gameObject;

        controlWristCenter = controlHaptics.transform.Find("WristCenter").gameObject;
        controlArmCenter = controlHaptics.transform.Find("ArmCenter").gameObject;

        targetHaptics.SetActive(false);


        while (!alive) {
            try {
                socketConnection = new TcpClient("raspberrypi.local", 5000);
                alive = true;
            }
            catch (SocketException socketException) {             
                Debug.Log("Socket exception: " + socketException);         
            }
        }

        try {  			
            clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
            clientReceiveThread.IsBackground = true; 			
            clientReceiveThread.Start();  
            		
        } 		
        catch (Exception e) { 			
            Debug.Log("On client connect exception " + e + ". \nTrying again."); 		
        }

        InvokeRepeating("SendMessage", 0.0f, 0.05f);
  

	} 

    int ComputeScore(float distance)
    {
        if (distance > 0.5) {
            return 0;
        }
        else if (distance < 0.01) {
            return 100;
        }
        else {
            float val = (0.5f - distance) / 0.5f * 100;
            return Mathf.RoundToInt(val);
        }
        
    }

	// Update is called once per frame
	void Update () { 
        // if (socketConnection != null) {
        //     // SendMessage();
        //     // ListenForData();  
        // }
        // else {
        //     Debug.Log("Socket connection not made yet!!");
        // } 
		
        

        Vector3 hmdRot = centerEyeAnchor.transform.rotation.eulerAngles;
        Vector3 hmdPos = centerEyeAnchor.transform.position;

        Vector3 humanRot = new Vector3(0.0f, hmdRot.y, 0.0f);
        Vector3 humanPos = new Vector3(hmdPos.x, hmdPos.y - headoffset, hmdPos.z);
        
        humanoid.transform.rotation = Quaternion.Euler(humanRot);
        humanoid.transform.position = humanPos;




        radius.rotation = Quaternion.Euler(Vector3.Scale(rotRad + radOffset, radCoef)); 
        humerus.rotation = Quaternion.Euler(Vector3.Scale(rotHum + humOffset, humCoef));



        if (OVRInput.GetUp(OVRInput.RawButton.X)) {
            haptics[15] = 101;
            Debug.Log("Sensors reset!");
        }
        else {
            haptics[15] = 0;
        }

        if (OVRInput.GetUp(OVRInput.RawButton.Y)) {
            Debug.Log("Target saved!");
            targetHaptics.SetActive(true);
            targetHaptics.transform.position = controlHaptics.transform.position;
            targetHaptics.transform.rotation = controlHaptics.transform.rotation;
            targetSet = true;
            // maxDistArm = Vector3.Distance(targetArmCenter.transform.position, controlArmCenter.transform.position);
            // maxDistWrist = Vector3.Distance(targetWristCenter.transform.position, controlWristCenter.transform.position);

            // haptics = new int[] {100, 100, 0, 0, 100, 100, 0, 0, 100, 100, 0, 0, 100, 100, 0, 0};
        }

        if (targetSet) {
            timer += Time.deltaTime;

            if (timer > 3.0) {
                int[] armInds = new int[] {0, 1, 4, 5};
                int[] wristInds = new int[] {8, 9, 12, 13};
                float armDist = Vector3.Distance(targetArmCenter.transform.position, controlArmCenter.transform.position);
                float wristDist = Vector3.Distance(targetWristCenter.transform.position, controlWristCenter.transform.position);

                foreach (int i in armInds) {
                    haptics[i] = Mathf.RoundToInt(ComputeScore(armDist));
                }
                foreach (int i in wristInds) {
                    haptics[i] = Mathf.RoundToInt(ComputeScore(wristDist));
                }
                Debug.Log(armDist + " " + maxDistArm);
                if (armDist < 0.05 && maxDistArm < 0.05) {
                    targetSet = false;
                    targetHaptics.SetActive(false);
                    timer = 0.0f;
                }
            }
            
        }
        else {
            haptics = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        }

        // radRef.transform.rotation = Quaternion.Euler(rotRad + radOffset); 
        // humRef.transform.rotation = Quaternion.Euler(Vector3.Scale(rotHum, humCoef) + humOffset); 
        
	} 

    private void ListenForData() { 		

        try {
            while (alive) {
                // Get a stream object for reading 				
                using (NetworkStream stream = socketConnection.GetStream()) { 					
                    int length; 	
                    Byte[] bytes = new Byte[128]; 				
                    // Read incomming stream into byte arrary. 					
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 						
                        var incommingData = new byte[length]; 						
                        Array.Copy(bytes, 0, incommingData, 0, length); 						
                        // Convert byte array to string message. 						
                        string serverMessage = Encoding.UTF8.GetString(incommingData); 						
                        // Debug.Log("server message received as: " + serverMessage);


                        string vectorString = serverMessage.Replace("(", "").Replace(")", ""); // Remove the parentheses
                        string[] vectorValues = vectorString.Split(','); // Split the string into individual values
                        float a1 = float.Parse(vectorValues[0].Trim()); // Parse the x component of the first vector
                        float b1 = float.Parse(vectorValues[1].Trim()); // Parse the y component of the first vector
                        float c1 = float.Parse(vectorValues[2].Trim()); // Parse the z component of the first vector
                        float d1 = float.Parse(vectorValues[3].Trim());

                        float a2 = float.Parse(vectorValues[4].Trim()); // Parse the x component of the second vector
                        float b2 = float.Parse(vectorValues[5].Trim()); // Parse the y component of the second vector
                        float c2 = float.Parse(vectorValues[6].Trim()); // Parse the z component of the second vector
                        float d2 = float.Parse(vectorValues[7].Trim());

                        Quaternion rotRadQuat = new Quaternion(b1, a1, c1, d1); // Create the first Vector3 object
                        Quaternion rotHumQuat = new Quaternion(b2, a2, c2, d2); // Create the second Vector3 object

                        rotRad = rotRadQuat.eulerAngles;
                        rotHum = rotHumQuat.eulerAngles;

                        // rotRad = new Vector3(rotRad.x, rotRad.z, rotRad.y);
                        // rotHum = new Vector3(rotHum.y, rotHum.x, rotHum.z);

                        // Debug.Log("Quaternions: " + rotRad + ", " + rotHum);


                    } 				
                } 	
            }
        }
        catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}   	
        		         
		    
	} 

    private void SendMessage() {         	
		
        try {
            // Get a stream object for writing. 			
            NetworkStream stream = socketConnection.GetStream(); 			
            if (stream.CanWrite) {                 
                
                string arrayString = "[" + string.Join(", ", haptics) + "]";
                int totalBytes = System.Text.Encoding.UTF8.GetByteCount(arrayString);
                int spacesNeeded = 128 - totalBytes;
                string spaces = new string(' ', spacesNeeded);
                string clientMessage = arrayString + spaces;
                // Debug.Log(clientMessage);

                // Convert string message to byte array.                 
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage); 				
                // Write byte array to socketConnection stream.                 
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);                 
                // Debug.Log("Client sent his message - should be received by server");             
            }  
        }  
        catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}       		
		   
	} 

    void OnApplicationQuit() {
        alive = false;
        clientReceiveThread.Abort();
        Debug.Log("Quit");
    }


}

