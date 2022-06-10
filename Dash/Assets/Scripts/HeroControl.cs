using System.Collections;
using UnityEngine;

public class HeroControl : MonoBehaviour
{

    #region DEFAULT /////////////////////////////////////////////////////////////////////////////////////

    private Vector2 _inputPlayer;
    [SerializeField] float _moveSpeed = 3f;
    [SerializeField] float _jumpForce = 6f;

    private int _facingDirection = 1;
    private Rigidbody2D _rb;
    private Animator _anim;

    [SerializeField] KeyCode _jumpButton = KeyCode.Space;
    [SerializeField] KeyCode _dashButton = KeyCode.LeftControl;

    [SerializeField] public bool _onGround;
    [SerializeField] Vector2 groundCheckBoxSize = Vector2.one;
    [SerializeField] Transform groundCheckObj;
    [SerializeField] public LayerMask groundMask;


    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();

        _playerGravityDefault = _rb.gravityScale;
    }


    private void Update()
    {
        if (_onDash == false) { InputPlayer(); }
        Reflect();
        HeroMove();

        if (Input.GetKeyDown(_jumpButton)) { Jump(); }
        if (Input.GetKeyDown(_dashButton)) { StartDash(); }
    }


    [SerializeField] ContactFilter2D _contactFilter;

    private void FixedUpdate()
    {
        CheckGround();
        if (_onGround == false && _rb.velocity.y <= 0) { _anim.SetBool("onFall", true); } 
        else { _anim.SetBool("onFall", false); }

        if (_onDash == true) { Dash(); }
    }


    private void CheckGround()
    {
        //_onGround = Physics2D.OverlapBox(groundCheckObj.position, groundCheckBoxSize, 0f, groundMask);
        _onGround = _mainCollider.IsTouching(_contactFilter);
        _anim.SetBool("onGround", _onGround);
    }

    


    private void InputPlayer()
    {
        _inputPlayer = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void Jump()
    {
        if (_onGround == true) { _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce); _anim.Play("Jump"); }
    }

    private void Reflect()
    {
        if (_inputPlayer.x != 0 && _inputPlayer.x != _facingDirection)
        {
            Vector3 temp = transform.localScale;
            temp.x *= -1;
            transform.localScale = temp;

            _facingDirection *= -1;
        }
    }

    private void HeroMove()
    {
        _rb.velocity = new Vector2(_inputPlayer.x * _moveSpeed, _rb.velocity.y);
        _anim.SetInteger("moveX", (int)Mathf.Abs(_inputPlayer.x));
    }

    #endregion


    #region DRAW ////////////////////////////////////////////////////////////////////////////////////////

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckObj.position, groundCheckBoxSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _dashDistance);
    }

    #endregion


    #region DASH ////////////////////////////////////////////////////////////////////////////////////////

    private float _playerGravityDefault;

    private bool _onDash = false;

    private Vector2 _dashCurrentPosition;
    private Vector2 _dashFinishPosition;

    [SerializeField] float _dashDistance = 3f;
    [SerializeField] float _dashSpeed = 3f;

    private float _dashProgress = 0f;

    [SerializeField] private float _dashTimeCooldown = 3f;
    private bool _dashReloaded = true;


    private void StartDash()
    {
        if (_dashReloaded == false) { return; }

        _dashCurrentPosition = transform.position;

        //if (_inputPlayer == Vector2.zero) { _dashFinishPosition = _dashCurrentPosition + _dashDistance * Vector2.right * _facingDirection; }
        //else { _dashFinishPosition = _dashCurrentPosition + _dashDistance * _inputPlayer.normalized; }
        Vector2 tempDirection = CalculateDashDirection();
        _dashFinishPosition = _dashCurrentPosition + _dashDistance * tempDirection;

        if (Mathf.Sign(tempDirection.x) != _facingDirection)
        {
            Vector3 temp = transform.localScale;
            temp.x *= -1;
            transform.localScale = temp;

            _facingDirection *= -1;
        }

        _dashProgress = 0f;

        _rb.gravityScale = 0;
        _rb.velocity = Vector2.zero;

        _dashReloaded = false;
        StartCoroutine(DashReloader());

        _onDash = true;
        _anim.SetBool("onDash", _onDash);
    }

    [SerializeField] private Collider2D _mainCollider;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        StopDash();
        
    }

    private Vector2 CalculateDashDirection()
    {
        return (Vector2)(Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position);
    }


    private void StopDash() // остановка –ывка при коллизи€х (например, когда перс упЄрс€ в стену)
    {
        // обнул€ем скорость, возвращаем гравитацию, и прерываем выполнение –ывка
        _rb.velocity = Vector2.zero;
        _rb.gravityScale = _playerGravityDefault;
        _onDash = false;
        _anim.SetBool("onDash", _onDash); // дл€ прерывани€ –ывка при коллизи€х ( ќѕ÷»ќЌјЋ№Ќќ! 2 )
    }


    IEnumerator DashReloader() // тупо "таймер перезар€дки" –ывка
    {
        yield return new WaitForSeconds(_dashTimeCooldown);
        _dashReloaded = true;
    }


    private void Dash() // метод рывка выполн€ющийс€ каждый FixedUpdate
    {
        _dashProgress += Time.fixedDeltaTime * _dashSpeed / _dashDistance; // рассчЄт прогресса выполнени€ –ывка за каждый FixedUpdate

        if (_dashProgress <= 1f) // если –ывок выполнен не на 100%, то двигаем перса, иначе - останавливаем –ывок
        {
            // ¬џ—„»“џ¬ј≈ћ куда должен переместитьс€ персонаж при текущем апдейте
            _dashCurrentPosition = Vector2.MoveTowards(transform.position, _dashFinishPosition, Time.fixedDeltaTime * _dashSpeed);
            _rb.MovePosition(_dashCurrentPosition); // двигаем перса на вычисленную позицию
            _anim.Play("Dash", 0, _dashProgress); // управл€ем анимацие –ывка ( ќѕ÷»ќЌјЋ№Ќќ! 3 )
            // пример: если анимаци€ состоит из 10 кадров, а текущий процесс == 30%, то воспроизведЄтс€ 3-й кадр
            // если процесс будет равен 70% - 7-й.
        }
        else
        {
            StopDash();
        }
    }

    #endregion

}