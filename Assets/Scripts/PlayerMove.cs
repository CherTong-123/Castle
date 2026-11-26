using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float repeatDelay = 0.2f; // 长按时，两步之间的最短时间间隔

    [SerializeField] private LayerMask obstacleLayers;
    private readonly RaycastHit2D[] hitResults = new RaycastHit2D[1];

    private Rigidbody2D rb;
    private Animator ani;

    private Vector2 targetPosition;
    private Vector2 direction;
    private bool isMoving;         // 是否正在走这一格
    private float nextMoveTime;    // 最早什么时候可以开始下一步
    public bool IsDialogueLocked { get; private set; }
    public bool IsMoving => isMoving;
    public bool IsDialoguePending { get; private set; }
    private RigidbodyConstraints2D constraintsBeforeDialogue;

    /// <summary>等待当前格走完时，只阻止下一步输入，不中断当前移动。</summary>
    public void SetDialoguePending(bool pending)
    {
        if (IsDialoguePending == pending) return;
        IsDialoguePending = pending;
        direction = Vector2.zero;
        nextMoveTime = 0f;
    }

    /// <summary>站定后对话，冻结当前位置，结束后恢复原来的物理约束。</summary>
    public void SetDialogueLocked(bool locked)
    {
        if (IsDialogueLocked == locked) return;
        IsDialogueLocked = locked;
        direction = Vector2.zero;
        nextMoveTime = 0f;
        if (locked)
        {
            constraintsBeforeDialogue = rb.constraints;
            rb.velocity = Vector2.zero;
            rb.constraints |= RigidbodyConstraints2D.FreezePosition;
            if (ani != null) ani.SetBool("isMoving", false);
        }
        else
        {
            rb.constraints = constraintsBeforeDialogue;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        targetPosition = rb.position;
        ani = GetComponent<Animator>();
        ani.SetFloat("InputX", 1);
        ani.SetBool("isMoving", false);
    }

    private void Update()
    {
        if (IsDialogueLocked || IsDialoguePending) return;
        direction = ReadDirection();

        if (direction == Vector2.zero)
        {
            nextMoveTime = 0f;
            return;
        }

        // 当前这一格还没走完，不能开始下一格。
        if (isMoving)
            return;

        // 长按时，等待重复间隔。
        if (Time.time < nextMoveTime)
            return;

        // 无论这次能否移动，都保持按键重复间隔。
        nextMoveTime = Time.time + repeatDelay;

        // 前方有障碍，这一步不启动。
        if (!CanMove(direction))
            return;

        targetPosition = rb.position + direction * gridSize;
        isMoving = true;
        nextMoveTime = Time.time + repeatDelay;
    }

    private void FixedUpdate()
    {
        if (IsDialogueLocked) return;
        if (!isMoving)
        {
            // 同一帧可能还有多次物理更新，走完后等待对话时不能带着速度漂移。
            if (IsDialoguePending) rb.velocity = Vector2.zero;
            return;
        }

        // 每次向目标格靠近一点。
        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);

        // 到达目标格，这一步结束。
        if ((nextPosition - targetPosition).sqrMagnitude < 0.000001f) isMoving = false;
    }

    private void LateUpdate()
    {
        if (IsDialogueLocked)
        {
            ani.SetBool("isMoving", false);
            return;
        }
        // 还按着有效方向，并且前方可以走。
        bool wantsToContinue =
            !IsDialoguePending && direction != Vector2.zero && CanMove(direction);

        // 正在走这一格，或者准备继续走，都播放走路动画。
        bool shouldPlayWalk = isMoving || wantsToContinue;

        ani.SetBool("isMoving", shouldPlayWalk);
    }

    private void OnDestroy()
    {
        if (IsDialogueLocked && rb != null) SetDialogueLocked(false);
    }

    private bool CanMove(Vector2 direction)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayers);
        filter.useTriggers = false;

        // 用玩家自身的碰撞体形状，检查前方一格。
        int hitCount = rb.Cast(
            direction,
            filter,
            hitResults,
            gridSize
        );

        return hitCount == 0;
    }
    private Vector2 ReadDirection()
    {
        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);

        if ((w && s) || (a && d))
            return Vector2.zero;

        // 按判断顺序决定优先级：上下优先于左右。
        if (w) return Vector2.up;
        if (s) return Vector2.down;
        if (a)
        {
            ani.SetFloat("InputX", -1);
            return Vector2.left;
        }
        if (d)
        {
            ani.SetFloat("InputX", 1);
            return Vector2.right;
        }

        return Vector2.zero;
    }
}
