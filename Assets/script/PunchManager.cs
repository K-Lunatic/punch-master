using UnityEngine;
using System.IO.Ports;
using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine.InputSystem;

public class PunchManager : MonoBehaviour
{
    SerialPort sp = new SerialPort("COM9", 115200); 
    
    [Header("게이지 컨트롤러")]
    public PowerGaugeController powerGauge; 
    public PowerGaugeController speedGauge; 
    public TextMeshProUGUI scoreText;

    [Header("사운드 설정")]
    public AudioSource audioSource;      
    public AudioClip swingSound;        
    public AudioClip hitSound;          
    public AudioClip scoreTickSound;    
    public AudioClip resetSound;        

    [Header("물리 한계치 (기존 수치 유지)")]
    private readonly float hardwareMaxG = 27.71f;      
    private readonly float hardwareMaxDps = 3464.1f;  
    public float punchThreshold = 3.0f; 

    [Header("연출 설정")]
    public float countDuration = 2.0f; 

    private bool canPunch = true; 
    private float peakAccel = 0;
    private float peakGyro = 0;
    private bool isPunching = false;
    private float timer = 0;
    private Coroutine countCoroutine;
    private float reconnectTimer = 0f;
    private float spaceChargeTime = 0f;
    private readonly float maxChargeTime = 2.0f;

    void Start() {
        AttemptOpenPort(); 
        SetSpriteScore(0);
    }

    void AttemptOpenPort() {
    if (sp.IsOpen) return;

    string[] ports = SerialPort.GetPortNames();

    foreach (string port in ports) {
        try {
            Debug.Log($"<color=white>[연결 시도]</color> {port} 확인 중...");
            
            sp.PortName = port;
            sp.DtrEnable = true; 
            sp.RtsEnable = true;
            sp.ReadTimeout = 500;
            sp.Open();

            string checkData = sp.ReadLine();
            if (checkData.Split(',').Length >= 7) {
                Debug.Log($"<color=green>● 하드웨어 찾음! 연결 포트: {port}</color>");
                sp.ReadTimeout = 20;
                return;
            }
        }
        catch {
            if (sp.IsOpen) sp.Close();
            continue;
        }
    }
}

    void Update() {
        HandleKeyboardInputs();

        if (!sp.IsOpen) {
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer > 2.0f) {
                reconnectTimer = 0f;
                AttemptOpenPort();
            }
            return; 
        }

        try {
            string data = sp.ReadLine(); 
            string[] v = data.Split(',');

            if (v.Length >= 7) {
                int btn = int.Parse(v[6]);
                if (btn == 1) { ResetGame(); return; }

                if (!canPunch) return;

                float ax = float.Parse(v[0], CultureInfo.InvariantCulture);
                float ay = float.Parse(v[1], CultureInfo.InvariantCulture);
                float az = float.Parse(v[2], CultureInfo.InvariantCulture);
                float gx = float.Parse(v[3], CultureInfo.InvariantCulture);
                float gy = float.Parse(v[4], CultureInfo.InvariantCulture);
                float gz = float.Parse(v[5], CultureInfo.InvariantCulture);

                float currentAccel = Mathf.Sqrt(ax * ax + ay * ay + az * az);
                float currentGyro = Mathf.Sqrt(gx * gx + gy * gy + gz * gz);

                if (currentAccel > punchThreshold && !isPunching) {
                    isPunching = true;
                    peakAccel = 0; peakGyro = 0; timer = 0;
                    if (swingSound != null) audioSource.PlayOneShot(swingSound);
                }

                if (isPunching) {
                    timer += Time.deltaTime;
                    if (currentAccel > peakAccel) peakAccel = currentAccel;
                    if (currentGyro > peakGyro) peakGyro = currentGyro;

                    if (timer > 0.3f) {
                        isPunching = false;
                        canPunch = false; 
                        StartFinalScoreSequence(peakAccel, peakGyro);
                    }
                }

                if (currentAccel > 1.1f)
                    Debug.Log($"<color=white>[Accel View]</color> <b>{currentAccel:F2}G</b> (Peak: {peakAccel:F2}G)");
            }
        } catch (TimeoutException) {
        } catch (Exception e) {
            if (!sp.IsOpen) AttemptOpenPort();
        }
    }

    private void HandleKeyboardInputs() {
        if (Keyboard.current == null) return;
        if (Keyboard.current.rKey.wasPressedThisFrame) ResetGame();
        if (!canPunch) return;

        if (Keyboard.current.spaceKey.isPressed) {
            if (spaceChargeTime == 0 && swingSound != null) audioSource.PlayOneShot(swingSound);
            spaceChargeTime += Time.deltaTime;
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame) {
            float chargeRatio = Mathf.Clamp01(spaceChargeTime / maxChargeTime);
            float fakeAccel = Mathf.Lerp(punchThreshold + 0.5f, hardwareMaxG, chargeRatio);
            float fakeGyro = Mathf.Lerp(500f, hardwareMaxDps, chargeRatio);
            canPunch = false;
            spaceChargeTime = 0f;
            StartFinalScoreSequence(fakeAccel, fakeGyro);
        }
    }

    void StartFinalScoreSequence(float accel, float gyro) {
        if (hitSound != null) audioSource.PlayOneShot(hitSound);
        float pRatio = Mathf.Clamp01((accel - punchThreshold - 1.0f) / (hardwareMaxG - punchThreshold - 1.0f));
        float sRatio = Mathf.Clamp01(gyro / hardwareMaxDps);
        int powerInt = Mathf.RoundToInt(pRatio * 10f);
        int speedInt = Mathf.RoundToInt(sRatio * 10f);
        int finalScore = Mathf.RoundToInt(((pRatio * 0.8f) + (sRatio * 0.2f)) * 99999f);
        powerGauge.PlayGaugeEffect(powerInt); 
        speedGauge.PlayGaugeEffect(speedInt); 
        if (countCoroutine != null) StopCoroutine(countCoroutine);
        countCoroutine = StartCoroutine(C_CountScore(finalScore));
    }

    private IEnumerator C_CountScore(int targetScore) {
        float elapsed = 0f;
        int lastDisplayedScore = 0;
        while (elapsed < countDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / countDuration;
            t = t * t * (3f - 2f * t); 
            int current = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, t));
            if (current != lastDisplayedScore) {
                SetSpriteScore(current);
                if (scoreTickSound != null && current % 10 == 0) audioSource.PlayOneShot(scoreTickSound, 0.3f);
                lastDisplayedScore = current;
            }
            yield return null;
        }
        SetSpriteScore(targetScore);
    }

    void SetSpriteScore(int score) {
        if (scoreText == null) return;
        string scoreStr = score.ToString("D5");
        string spriteText = "";
        foreach (char c in scoreStr) { spriteText += $"<sprite name=\"{c}\">"; }
        scoreText.text = spriteText;
    }

    public void ResetGame() {
        if (countCoroutine != null) StopCoroutine(countCoroutine);
        if (resetSound != null) audioSource.PlayOneShot(resetSound);
        canPunch = true;
        peakAccel = 0; peakGyro = 0; isPunching = false;
        spaceChargeTime = 0f;
        SetSpriteScore(0);
        powerGauge.SetGauge(0);
        speedGauge.SetGauge(0);
        Debug.Log("<color=cyan>● 준비 완료!</color>");
    }
}
