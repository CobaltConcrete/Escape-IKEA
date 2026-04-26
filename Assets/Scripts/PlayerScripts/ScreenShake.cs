using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private Vector3 shakeOffset;
    private Coroutine shakeRoutine;

    public Vector3 GetShakeOffset() => shakeOffset;

    private void Awake()
    {
        Instance = this;
    }

    public void Shake(float duration, float strength)
    {
        Debug.Log("SCREEN SHAKE CALLED: " + duration + ", " + strength);

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(CoShake(duration, strength));
    }

    private IEnumerator CoShake(float duration, float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float falloff = 1f - (timer / duration); // ½¥Èõ
            Vector2 offset = Random.insideUnitCircle * strength * falloff;

            shakeOffset = new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        shakeOffset = Vector3.zero;
        shakeRoutine = null;
    }
}