using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System;

public class SerialIMUReader : MonoBehaviour
{
    public string portName = "COM3"; 
    public int baudRate = 115200;
    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;
    public float[] imuValues = new float[6];

    void Start()
    {
        OpenSerialPort();
    }

    void OpenSerialPort()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50;
            serialPort.Open();
            isRunning = true;
            readThread = new Thread(ReadSerialData);
            readThread.IsBackground = true;
            readThread.Start();
            Debug.Log("시리얼 포트 연결 성공!");
        }
        catch (Exception e)
        {
            Debug.LogError("시리얼 포트 연결 에러: " + e.Message);
        }
    }

    void ReadSerialData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string dataString = serialPort.ReadLine();
                string[] dataParts = dataString.Split(',');
                if (dataParts.Length == 6)
                    for (int i = 0; i < 6; i++)
                        float.TryParse(dataParts[i], out imuValues[i]);
            }
            catch (TimeoutException) {}
            catch (Exception e) { Debug.LogWarning("읽기 에러: " + e.Message); }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (readThread != null && readThread.IsAlive) readThread.Join();
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
    }
}