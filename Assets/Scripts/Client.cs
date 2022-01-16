using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InputPayload
{
    public int tick;
    public Vector3 inputVector;
}

public struct StatePayload
{
    public int tick;
    public Vector3 position;
}

public class Client : MonoBehaviour
{
    public static Client Instance;

    // Shared
    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;

    private StatePayload[] stateBuffer;
    private InputPayload[] inputBuffer;
    private StatePayload latestServerState;
    private StatePayload lastProcessedState;
    private float horizontal;
    private float vertical;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputBuffer = new InputPayload[BUFFER_SIZE];
    }

    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        if (horizontal == 0 && vertical == 0)
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

    public void OnServerMovementState(StatePayload serverState)
    {
        latestServerState = serverState;
    }

    IEnumerator SendToServer(InputPayload inputPayload)
    {
        yield return new WaitForSeconds(0.02f);

        Server.Instance.OnClientInput(inputPayload);
    }

    void HandleTick()
    {
        // Do things in Tick
        if (!latestServerState.Equals(default(StatePayload)) && (lastProcessedState.Equals(default(StatePayload)) || !latestServerState.Equals(lastProcessedState)))
        {
            HandleServerReconciliation();
        }

        HandleInput();
    }

    void HandleInput()
    {
        int bufferIndex = currentTick % BUFFER_SIZE;

        // Add payload to inputBuffer
        InputPayload inputPayload = new InputPayload();
        inputPayload.tick = currentTick;
        inputPayload.inputVector = new Vector3(horizontal, 0, vertical);
        inputBuffer[bufferIndex] = inputPayload;

        // Add payload to stateBuffer
        stateBuffer[bufferIndex] = ProcessMovement(inputPayload);

        // Send input to server
        StartCoroutine(SendToServer(inputPayload));
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        // Should always be in sync with same function on Server
        transform.position += input.inputVector * 5f * minTimeBetweenTicks;

        return new StatePayload()
        {
            tick = input.tick,
            position = transform.position,
        };
    }

    void HandleServerReconciliation()
    {
        lastProcessedState = latestServerState;

        int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
        float positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].position);

        if (positionError > 0.001f)
        {
            Debug.Log("We have to reconcile bro");
            // Rewind & Replay
            transform.position = latestServerState.position;

            // Update buffer at index of latest server state
            stateBuffer[serverStateBufferIndex].position = transform.position;

            // Now re-simulate the rest of the ticks up to the current tick on the client
            int tickToProcess = latestServerState.tick + 1;

            while (tickToProcess < currentTick)
            {
                // Process new movement with reconciled state
                StatePayload statePayload = ProcessMovement(inputBuffer[tickToProcess]);

                // Update buffer with recalculated state
                int bufferIndex = tickToProcess % BUFFER_SIZE;
                stateBuffer[bufferIndex] = statePayload;

                tickToProcess++;
            }
        }
    }
}
