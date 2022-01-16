using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour
{
    public static Server Instance;

    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;

    private Queue<InputPayload> inputQueue;
    private StatePayload[] stateBuffer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputQueue = new Queue<InputPayload>();
    }

    void Update()
    {
        if (inputQueue.Count == 0)
        {
            return;
        }

        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            HandleTick();
            currentTick++;
        }
    }

    public void OnClientInput(InputPayload inputPayload)
    {
        inputQueue.Enqueue(inputPayload);
    }

    IEnumerator SendToClient(StatePayload statePayload)
    {
        yield return new WaitForSeconds(0.02f);

        Client.Instance.OnServerMovementState(statePayload);
    }

    void HandleTick()
    {
        // Process the input queue
        int bufferIndex = -1;
        while(inputQueue.Count > 0)
        {
            InputPayload inputPayload = inputQueue.Dequeue();

            bufferIndex = inputPayload.tick % BUFFER_SIZE;

            StatePayload statePayload = ProcessMovement(inputPayload);
            stateBuffer[bufferIndex] = statePayload;
        }

        if (bufferIndex != -1)
        {
            StartCoroutine(SendToClient(stateBuffer[bufferIndex]));
        }
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        // Should always be in sync with same function on Client
        transform.position += input.inputVector * 5f * minTimeBetweenTicks;

        return new StatePayload()
        {
            tick = input.tick,
            position = transform.position,
        };
    }
}
