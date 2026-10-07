using UnityEngine;

public class WeaponArbiter : MonoBehaviour
{
    public Weapon Active { get; private set; }

    // อาวุธที่ถือสิทธิ์อยู่ทำงานต่อได้ปกติ อีกอันต้องรอจนกว่าตัวที่ถืออยู่จะปล่อย
    // แล้วจะได้สิทธิ์ทันทีในเฟรมถัดไป ไม่ต้องปล่อยปุ่มแล้วกดใหม่
    public bool RequestControl(Weapon requester, bool wantsControl)
    {
        if (Active == requester)
        {
            if (!wantsControl) Active = null;
            return wantsControl;
        }

        if (Active == null && wantsControl)
        {
            Active = requester;
            return true;
        }

        return false;
    }
}