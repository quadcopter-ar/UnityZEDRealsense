using System.Net;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
// using UnityEngine.InputSystem;
using System;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;
using System.Collections;
using TMPro;


[Serializable]
public class PoseJSON
{
    public float x;
    public float y;
    public float z;
    public float i;
    public float j;
    public float k;
    public float w;
}

public class RSMovement : MonoBehaviour
{
    // Start is called before the first frame update
    public bool debugMode = false;

    [SerializeField] private TextMeshProUGUI myText;
    public string remoteIP;
    public GameObject droneObject;

    private Thread clientRecieveThread;
    private TcpClient socketConnection;

    private Vector3 target_position = new Vector3(0, 0, 0);
    // private Vector3 target_orientation = new Vector3(0, 0, 0);
    private Quaternion target_quaternion = new Quaternion(0, 0, 0, 0);

    void Start()
    {
        // Starting client when the game starts up
        ConnectToTcpServer();
    }

    void Update()
    {
       // Updating the players positon
        transform.position = target_position;
        transform.rotation = target_quaternion;
        // Debug.Log("Position: " + transform.position + " Rotation: " + transform.rotation);
    }


    private void ConnectToTcpServer()
    {
        try
        {
            // Start background thread to fetch data while game play is current
            clientRecieveThread = new Thread(new ThreadStart(ListenForData));
            clientRecieveThread.IsBackground = true;
            clientRecieveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("Error: Connection Error occured on TCP link.");
        }
    }

    private void ListenForData()
    {
        try
        {
            socketConnection = new TcpClient(remoteIP, 13579);

            if (socketConnection.Connected)
                Debug.Log("Error: TCP Server Connected.");
            while (true)
            {
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    // Buffer may hold partial data, 256 allows extra space to ensure
                    // there a set of valid data in the buffer.
                    Byte[] bytes = new byte[256];

                    while (true)
                    {
                        try
                        {
                            stream.Read(bytes, 0, bytes.Length);
                            string buffer_str = Encoding.UTF8.GetString(bytes);
                            int start = buffer_str.IndexOf('{');
                            int end = buffer_str.IndexOf('}', start);
                            // get pose data out of buffer
                            string pose_str = buffer_str.Substring(start, end - start + 1);
                            Debug.Log(pose_str);
                            PoseJSON p = JsonUtility.FromJson<PoseJSON>(pose_str);
                            // Setting postional data to be applied on Update
                            target_position.x = p.x * 100;
                            target_position.y = p.y * 100;
                            target_position.z = -p.z * 100;
                            // Setting rotational data to be applied on Update
                            target_quaternion.x = -p.i;
                            target_quaternion.y = -p.j;
                            target_quaternion.z = p.k;
                            target_quaternion.w = p.w;
                            
                            // If using Euler Angles
                            /*
                            target_orientation.x = -p.i * 180f / Mathf.PI;
                            target_orientation.y = p.j * 180f / Mathf.PI;
                            target_orientation.z = p.j * 180f / Mathf.PI;
                            */
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                            stream.Flush();
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    void OnDestroy()
    {
        clientRecieveThread.Abort();
        Debug.Log("OnDestroy1");
    }
}

