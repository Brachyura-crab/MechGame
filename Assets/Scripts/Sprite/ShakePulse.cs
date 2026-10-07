using UnityEngine;

public class ShakePulse
{
    readonly float burstAmplitude, burstDecay, sustainAmplitude, frequency;
    float burstStrength;
    bool wasActive;

    public ShakePulse(float burstAmplitude, float burstDecay, float sustainAmplitude, float frequency)
    {
        this.burstAmplitude = burstAmplitude;
        this.burstDecay = burstDecay;
        this.sustainAmplitude = sustainAmplitude;
        this.frequency = frequency;
    }

    // เรียกทุกเฟรม คืนตำแหน่งที่เขยื้อนในกรอบ -1..1 x -1..1 คูณความแรงปัจจุบันแล้ว
    public Vector2 Update(bool active, float seed)
    {
        if (active && !wasActive) burstStrength = burstAmplitude;   // เพิ่งเริ่ม boost: ยิง burst
        wasActive = active;

        burstStrength = Mathf.MoveTowards(burstStrength, 0f, burstDecay * Time.deltaTime);
        float sustain = active ? sustainAmplitude : 0f;
        float strength = burstStrength + sustain;

        if (strength <= 0.0001f) return Vector2.zero;

        float t = Time.time * frequency;
        float x = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f;
        return new Vector2(x, y) * strength;
    }
}