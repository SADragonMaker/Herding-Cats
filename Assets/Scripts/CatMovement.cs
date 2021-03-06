using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CatState
{
    Idle,
    Walk,
    ScaredByDog,
    ScaredByWolf
}

public class CatMovement : MonoBehaviour
{

    [Header("External References")]
    public GameObject dog;
    private GameObject wolf;

    [Header("Movement Variables")]
    public float movementSpeed;
    private Vector2 target;         //coordinates the cat is attempting to move towards
    public float stateChangeDelay;
    public float chaseThreshold;    //distance dog has to be to chase this cat

    [Header("Map Boundaries")]
    public Vector2 xRange;
    public Vector2 yRange;

    [Header("Behaviour")]
    public float annoyingness;

    [HideInInspector]
    public bool insideZone = false;
    //Internal References/Variables
    private Animator anim;
    private Rigidbody2D rb;
    public CatState state;
    private float timeToChangeState;
    private Vector2 currentRandomTarget; //this is set each time the cat exits idle mode

    private Vector2 animationVector = new Vector2(0.0f, 0.0f);

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        state = CatState.Walk;
        wolf = null;
    }

    void AdjustTime() {
        timeToChangeState = Time.time + stateChangeDelay * Random.Range(0.8f, 1.2f);
    }

    // Update is called once per frame
    void Update()
    {
        //set state to running if the dog is close no matter what 
        //Magnitude calulations must be based on x/y only, not z!
        Vector2 dogLoc = new Vector2(dog.transform.position.x, dog.transform.position.y);
        Vector2 wolfLoc;
        Vector2 catLoc = new Vector2(transform.position.x, transform.position.y);

        bool hearingDogBark = (dog.GetComponent<DogMovement>().IsDogBarking() && (dogLoc - catLoc).magnitude < chaseThreshold * 1.6f);
        if (((dogLoc - catLoc).magnitude < chaseThreshold || hearingDogBark) && state != CatState.ScaredByWolf)
        {
            state = CatState.ScaredByDog;
        }
        if (wolf != null)
        {
            wolfLoc = new Vector2(wolf.transform.position.x, wolf.transform.position.y);
            if (((wolfLoc - catLoc).magnitude < chaseThreshold) && state != CatState.ScaredByDog)
            {
                state = CatState.ScaredByWolf;
            }
        }
        else {
            wolfLoc = Vector2.zero;
        }
        //set the target location based on state
        switch (state)
        {
            case CatState.Walk:
                target = currentRandomTarget;
                if (Time.time > timeToChangeState)
                {
                    AdjustTime();
                    //TODO set target to the randomly decided location
                    state = CatState.Idle;
                }
                break;

            case CatState.Idle:
                target = transform.position;
                if (Time.time > timeToChangeState)
                {
                    AdjustTime();
                    if(Random.Range(0f, 1f) < annoyingness) {
                        SetNewRandomLocation();
                        state = CatState.Walk;
                    }
                }
                break;

            case CatState.ScaredByDog:
                //if the cat is far enough away from teh dog, stop running and wander-
                if ((dogLoc - catLoc).magnitude > chaseThreshold && !hearingDogBark)
                {
                    //SetNewRandomLocation();
                    AdjustTime();
                    state = CatState.Idle;
                }
                target = catLoc + (catLoc - dogLoc) * movementSpeed / (catLoc - dogLoc).magnitude;
                break;
            case CatState.ScaredByWolf:
                if ((wolfLoc - catLoc).magnitude > chaseThreshold) {
                    AdjustTime();
                    state = CatState.Idle;
                }
                target = catLoc + (catLoc - wolfLoc) * movementSpeed / (catLoc - wolfLoc).magnitude;
                break;
        }
        if (wolf == null) {
            CheckWolf();
        }
        UpdateAnimator();
    }

    private void SetNewRandomLocation()
    {
        float xRand = Random.Range(xRange.x, xRange.y);
        float yRand = Random.Range(yRange.x, yRange.y);
        currentRandomTarget = new Vector2(xRand, yRand);
    }

    //this is just called at the end of every frame
    private void UpdateAnimator()
    {
        //TODO change the cat bool flags based on it's current x and y velocity
        //query rb.velocity.x and rb.velocity.y
        Vector2 distance = target - (Vector2)transform.position;
        animationVector = Vector2.Lerp(animationVector, rb.velocity, 2f * Time.deltaTime);
        if (distance.magnitude > 0)
        {
            anim.SetFloat("Vertical", animationVector.y);
            anim.SetFloat("Horizontal", animationVector.x);
            if (state == CatState.Walk)
            {
                rb.velocity = distance * (distance.magnitude > movementSpeed / 10 ? movementSpeed / distance.magnitude : 0);
            }
            else
            {
                rb.velocity = distance * (distance.magnitude > movementSpeed / 10 ? 1.6f * movementSpeed / distance.magnitude : 0);
            }
        }
        else {
            rb.velocity = Vector2.zero;
        }

        switch (state)
        {
            case CatState.Idle:
                if (rb.velocity.magnitude > 0) {
                    anim.SetFloat("SpeedMult", 0.2f);
                }
                break;
            case CatState.Walk:
                anim.SetFloat("SpeedMult", 0.3f);
                break;
            case CatState.ScaredByDog:
                anim.SetFloat("SpeedMult", 0.5f);
                break;
            case CatState.ScaredByWolf:
                anim.SetFloat("SpeedMult", 0.5f);
                break;
        }

        anim.SetFloat("Magnitude", rb.velocity.magnitude);
    }

    public void CheckWolf() {
        if (GameObject.FindGameObjectWithTag("Wolf") != null) {
            wolf = GameObject.FindGameObjectWithTag("Wolf");
        }
    }
}