
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class RunningSystem : UdonSharpBehaviour
{

    public float accelerationDeadZoneLower = .5f;
    public float accelerationDeadZoneUpper = 4f;
    public float velocityMultiplyer = 10f;
    public float maxSpeed = 20f;
    public float accelerationSmoothTime = .1f;
    public float playerSpeedSmoothTime = .7f;
    public float maxStopTime = .2f;//Some time amplitude will be very small for a very short amount of time

    Vector3 headSmoothedLocalAcceleration, headSmoothedLocalVelocity;
    Vector3 headOldLocalPosition, headOldLocalVelocity;
    float estimatedSpeed;
    float avatarHeight = 1.5f;
    float stopTime;
    bool stopped = true;

    private void Start()
    {
        headOldLocalPosition = GetHeadLocalPosition();
    }

    Vector3 GetHeadLocalPosition()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        Vector3 playerPosition = localPlayer.GetPosition();
        Quaternion playerRotation = localPlayer.GetRotation();
        VRCPlayerApi.TrackingData headData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

        return Quaternion.Inverse(playerRotation) * (headData.position - playerPosition);
    }

    private void FixedUpdate()
    {

        Vector3 headLocalPosition = GetHeadLocalPosition();
        Vector3 headLocalVelocity = (headLocalPosition - headOldLocalPosition) / Time.fixedDeltaTime;
        Vector3 headLocalAcceleration = (headLocalVelocity - headOldLocalVelocity) / Time.fixedDeltaTime;
        headSmoothedLocalAcceleration = Vector3.Lerp(headSmoothedLocalAcceleration, headLocalAcceleration, Time.fixedDeltaTime / accelerationSmoothTime);
        headSmoothedLocalVelocity = Vector3.Lerp(headSmoothedLocalVelocity, headLocalVelocity, Time.fixedDeltaTime / accelerationSmoothTime);
        float acceleration = headSmoothedLocalAcceleration.y;
        float velocity = headSmoothedLocalVelocity.y;
        float amplitude = Mathf.Sqrt(acceleration * acceleration + velocity * velocity * 355.30576f);

        avatarHeight = Mathf.Lerp(avatarHeight, Mathf.Max(.3f, headLocalPosition.y), Time.deltaTime / 2f);

        if (stopped)
        {
            if(amplitude > accelerationDeadZoneUpper * avatarHeight)
            {
                stopped = false;
                stopTime = 0;
            }
        }
        if (!stopped)
        {
            if (amplitude < accelerationDeadZoneLower * avatarHeight)
            {
                stopTime += Time.fixedDeltaTime;
                if (stopTime > maxStopTime)
                {
                    stopped = true;
                    estimatedSpeed = 0;
                }
            }
        }
        if(!stopped)
            estimatedSpeed = Mathf.Lerp(estimatedSpeed, amplitude * 0.05305f, Time.fixedDeltaTime / playerSpeedSmoothTime);


        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        float speed = Mathf.Min(maxSpeed, estimatedSpeed * velocityMultiplyer);
        localPlayer.SetWalkSpeed(speed);
        localPlayer.SetRunSpeed(speed);
        localPlayer.SetStrafeSpeed(speed);

        headOldLocalPosition = headLocalPosition;
        headOldLocalVelocity = headLocalVelocity;
        if(debugRawImage)
            if (++elapsed >= 2)
            {
                UpdateOscilloscope(accelerationDeadZoneUpper * avatarHeight / 10, amplitude / 10, velocity,acceleration/10);
            }
    }
    #region Debug
    
    int elapsed;
    public RawImage debugRawImage;//128x64, ARGB32, write enabled
    int curX;
    void UpdateOscilloscope(float val1, float val2, float val3, float val4)
    {
        Texture2D texture=(Texture2D)debugRawImage.mainTexture;
        for (int i = 0; i < 128; ++i)
            texture.SetPixel(curX, i, Color.white);
        texture.SetPixel(curX, 32, Color.grey);
        texture.SetPixel(curX, 64, Color.black);
        texture.SetPixel(curX, 96, Color.grey);
        int y;
        y = Mathf.Clamp((int)(64 + 64 * val1), 0, 128);
        texture.SetPixel(curX, y, Color.red);
        y = Mathf.Clamp((int)(64 + 64 * val2), 0, 128);
        texture.SetPixel(curX, y, Color.green);
        y = Mathf.Clamp((int)(64 + 64 * val3), 0, 128);
        texture.SetPixel(curX, y, Color.blue);
        y = Mathf.Clamp((int)(64 + 64 * val4), 0, 128);
        texture.SetPixel(curX, y, Color.yellow);
        texture.Apply();
        ++curX;
    }
    
    #endregion
}
