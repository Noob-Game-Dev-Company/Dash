using System.Collections;
using UnityEngine;

public class HeroControl : MonoBehaviour
{

    #region DEFAULT /////////////////////////////////////////////////////////////////////////////////////

    private Vector2 _inputPlayer;
    [SerializeField] float _moveSpeed = 3f;
    [SerializeField] float _jumpForce = 6f;

    private int _facingDirection = 1; // ��� ����������� ����� (� �� �������, ���� ������� ����), ���� ��� ����� �� ������
    private Rigidbody2D _rb;
    private Animator _anim;

    [SerializeField] KeyCode _jumpButton = KeyCode.Space;
    [SerializeField] KeyCode _dashButton = KeyCode.LeftControl; // ������� �����

    [SerializeField] public bool _onGround;
    [SerializeField] Vector2 groundCheckBoxSize = Vector2.one;
    [SerializeField] Transform groundCheckObj;
    [SerializeField] public LayerMask groundMask;


    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();

        _playerGravityDefault = _rb.gravityScale; // ��� �������������� ��������� ���������� ��� ��� ���������� ( �����������! 1 )
    }


    private void Update()
    {
        InputPlayer();
        Reflect();
        HeroMove();

        if (Input.GetKeyDown(_jumpButton)) { Jump(); }
        if (Input.GetKeyDown(_dashButton)) { StartDash(); } // ����� ������ ��� ���������� � �����
    }


    private void FixedUpdate()
    {
        CheckGround();
        if (_onGround == false && _rb.velocity.y <= 0) { _anim.SetBool("onFall", true); } 
        else { _anim.SetBool("onFall", false); }

        if (_onDash == true) { Dash(); } // ����� ������ �����
    }


    private void CheckGround()
    {
        _onGround = Physics2D.OverlapBox(groundCheckObj.position, groundCheckBoxSize, 0f, groundMask);
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

            _facingDirection *= -1; // �������� ����� ��� �����
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
        Gizmos.DrawWireSphere(transform.position, _dashDistance); // ��������� ��������� �����
    }

    #endregion


    #region DASH ////////////////////////////////////////////////////////////////////////////////////////

    private float _playerGravityDefault; // ��� ���������� ��������� ���������� ����� ������ ( �����������! 1 )

    private bool _onDash = false; // ������� �����

    private Vector2 _dashCurrentPosition; // ����������� ��������� ������� ��� �����
    private Vector2 _dashFinishPosition; // ������� � ������� ������ ��������� ���� ����� �����

    [SerializeField] float _dashDistance = 3f; // ��������� �����
    [SerializeField] float _dashSpeed = 3f; // �������� �����

    private float _dashProgress = 0f; // ������� ������� ���������� �����

    [SerializeField] private float _dashTimeCooldown = 3f; // ����� ��� ����������� ������ "�����"
    private bool _dashReloaded = true; // ���������� ��������� ����� (��������� �����������)


    private void StartDash() // ��������� ����� ������
    {
        if (_dashReloaded == false) { return; } // �����, ���� ����������� ��� �� ���������

        _dashCurrentPosition = transform.position;

        // ���� ��� ����� �� ������ - �������� � ����������� ������� ���������
        if (_inputPlayer == Vector2.zero) { _dashFinishPosition = _dashCurrentPosition + _dashDistance * Vector2.right * _facingDirection; }
        // ���� ���� ����, �� �������� �� ����������� �����
        else { _dashFinishPosition = _dashCurrentPosition + _dashDistance * _inputPlayer.normalized; }
        
        _dashProgress = 0f; // ���������� �������� �� ����������� �����

        _rb.gravityScale = 0; // ��������� ���������� ( �����������! 1 )
        _rb.velocity = Vector2.zero; // �������� ��������� ��������� ��������

        _dashReloaded = false; // ������ ����� �� �����������
        StartCoroutine(DashReloader()); // ��������� ������ ��� ����������� �����

        _onDash = true; // �� ��������� ������ ���������, ���������� ������� �����
        _anim.SetBool("onDash", _onDash); // ��� ���������� ����� ��� ��������� ( �����������! 2 )
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        StopDash(); // ��� ��������� ������������� ����� ( �����������! 2 )
    }


    private void StopDash() // ��������� ����� ��� ��������� (��������, ����� ���� ����� � �����)
    {
        // �������� ��������, ���������� ����������, � ��������� ���������� �����
        _rb.velocity = Vector2.zero;
        _rb.gravityScale = _playerGravityDefault;
        _onDash = false;
        _anim.SetBool("onDash", _onDash); // ��� ���������� ����� ��� ��������� ( �����������! 2 )
    }


    IEnumerator DashReloader() // ���� "������ �����������" �����
    {
        yield return new WaitForSeconds(_dashTimeCooldown);
        _dashReloaded = true;
    }


    private void Dash() // ����� ����� ������������� ������ FixedUpdate
    {
        _dashProgress += Time.fixedDeltaTime * _dashSpeed / _dashDistance; // ������� ��������� ���������� ����� �� ������ FixedUpdate

        if (_dashProgress <= 1f) // ���� ����� �������� �� �� 100%, �� ������� �����, ����� - ������������� �����
        {
            // ����������� ���� ������ ������������� �������� ��� ������� �������
            _dashCurrentPosition = Vector2.MoveTowards(transform.position, _dashFinishPosition, Time.fixedDeltaTime * _dashSpeed);
            _rb.MovePosition(_dashCurrentPosition); // ������� ����� �� ����������� �������
            _anim.Play("Dash", 0, _dashProgress); // ��������� �������� ����� ( �����������! 3 )
            // ������: ���� �������� ������� �� 10 ������, � ������� ������� == 30%, �� �������������� 3-� ����
            // ���� ������� ����� ����� 70% - 7-�.
        }
        else
        {
            StopDash();
        }
    }

    #endregion

}