using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using UnityEngine;



public class rotateIMU_OLD : MonoBehaviour
{

    private TcpClient socketConnection; 	
	private Thread clientReceiveThread; 

    private volatile bool alive = false;

    private Vector3 rotRad;
    private Vector3 rotHum;


    void Start () {
        alive=true;
		ConnectToTcpServer();     
	}  	
	// Update is called once per frame
	void Update () {         
		if (Input.GetKeyDown(KeyCode.Space)) {             
			// SendMessage(); 
            Debug.Log("Space pressed");       
		}    

        transform.rotation = Quaternion.Euler(rotRad); 
	} 

    private void ConnectToTcpServer () { 
        
        try {  			
            clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
            clientReceiveThread.IsBackground = true; 			
            clientReceiveThread.Start();  
            		
        } 		
        catch (Exception e) { 			
            Debug.Log("On client connect exception " + e + ". \nTrying again."); 		
        }
        	
		 	
	}  

    private void ListenForData() { 		
		try { 			
			socketConnection = new TcpClient("raspberrypi.local", 5000);  			            
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
						Debug.Log("server message received as: " + serverMessage);


                        string vectorString = serverMessage.Replace("(", "").Replace(")", ""); // Remove the parentheses
                        string[] vectorValues = vectorString.Split(','); // Split the string into individual values
                        float x1 = float.Parse(vectorValues[0].Trim()); // Parse the x component of the first vector
                        float y1 = float.Parse(vectorValues[1].Trim()); // Parse the y component of the first vector
                        float z1 = float.Parse(vectorValues[2].Trim()); // Parse the z component of the first vector
                        float x2 = float.Parse(vectorValues[3].Trim()); // Parse the x component of the second vector
                        float y2 = float.Parse(vectorValues[4].Trim()); // Parse the y component of the second vector
                        float z2 = float.Parse(vectorValues[5].Trim()); // Parse the z component of the second vector
                        
                        rotRad = new Vector3(x1, y1, z1); // Create the first Vector3 object
                        rotHum = new Vector3(x2, y2, z2); // Create the second Vector3 object
                        Debug.Log("VECTORS: " + rotRad + ", " + rotHum);


					} 				
				} 			
			}         
		}         
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	} 

    private void SendMessage() {         
		if (socketConnection == null) {             
			return;         
		}  		
		try { 			
			// Get a stream object for writing. 			
			NetworkStream stream = socketConnection.GetStream(); 			
			if (stream.CanWrite) {                 
				int[] myArray = new int[] {100, 200, 300, 400, 500};
                string arrayString = "[" + string.Join(", ", myArray) + "]";
                int totalBytes = System.Text.Encoding.UTF8.GetByteCount(arrayString);
                int spacesNeeded = 128 - totalBytes;
                string spaces = new string(' ', spacesNeeded);
                string clientMessage = arrayString + spaces;
                Debug.Log(clientMessage);

				// Convert string message to byte array.                 
				byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage); 				
				// Write byte array to socketConnection stream.                 
				stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);                 
				Debug.Log("Client sent his message - should be received by server");             
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

