using UnityEngine;
using Cinemachine;

public class SlowTime : Item
{
    private float fixedDeltaTime;

    public CinemachineVirtualCamera carCam;
    public CinemachineVirtualCamera closeCam;

    void Awake()
    {
        this.fixedDeltaTime = Time.fixedDeltaTime;
    }

    public override void UseItem()
    {
        if (Time.timeScale == 1.0f)
        {
            Time.timeScale = 0.5f;
            closeCam.Priority = 20;
            carCam.Priority = 10;
        }
        else
        {
            Time.timeScale = 1.0f;
            carCam.Priority = 20;
            closeCam.Priority = 10;
        }

        Time.fixedDeltaTime = this.fixedDeltaTime * Time.timeScale;
    }
}
