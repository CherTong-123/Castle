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
        if (!isMoving)
            return;

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
        // 还按着有效方向，并且前方可以走。
        bool wantsToContinue =
            direction != Vector2.zero && CanMove(direction);

        // 正在走这一格，或者准备继续走，都播放走路动画。
        bool shouldPlayWalk = isMoving || wantsToContinue;

        ani.SetBool("isMoving", shouldPlayWalk);
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