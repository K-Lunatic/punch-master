using UnityEngine;
using System.Collections;

public class PowerGaugeController : MonoBehaviour
{
    [Header("설정")]
    public Animator gaugeAnimator;
    public string parameterName = "Power";

    public void PlayGaugeEffect(float targetScore)
    {
        StopAllCoroutines();
        int finalIntScore = Mathf.RoundToInt(targetScore);
        StartCoroutine(C_AnimateGaugeInt(finalIntScore));
    }

    private IEnumerator C_AnimateGaugeInt(int targetPower)
    {
        int currentDisplayPower = 0;
        Debug.Log($"애니메이션 시작! 목표 점수: {targetPower}");
        float stepDelay = 0.05f;

        while (currentDisplayPower < targetPower)
        {
            currentDisplayPower++;
            gaugeAnimator.SetInteger(parameterName, currentDisplayPower);
            yield return new WaitForSeconds(stepDelay);
        }

        gaugeAnimator.SetInteger(parameterName, targetPower);
    }

    public void SetGauge(int value)
    {
        if (gaugeAnimator != null)
            gaugeAnimator.SetInteger(parameterName, value);
    }
}