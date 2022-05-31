using System.Collections;
using UnityEngine;

public class HeroControl : MonoBehaviour
{

    private Vector2 _inputPlayer;
    [SerializeField] float _moveSpeed = 3f;
    [SerializeField] float _jumpForce = 6f;

    private int _facingDirection = 1; // для направления Рывка (в ту сторону, куда смотрит перс), если нет ввода от игрока
    private Rigidbody2D _rb;
    private Animator _anim;

    [SerializeField] KeyCode _jumpButton = KeyCode.Space;
    [SerializeField] KeyCode _dashButton = KeyCode.LeftControl; // клавиша Рывка

    [SerializeField] public bool _onGround;
    [SerializeField] Vector2 groundCheckBoxSize = Vector2.one;
    [SerializeField] Transform groundCheckObj;
    [SerializeField] public LayerMask groundMask;


    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();

        _playerGravityDefault = _rb.gravityScale; // для восстановления множителя гравитации при его отключении
    }


    private void Update()
    {
        InputPlayer();
        Reflect();
        HeroMove();

        if (Input.GetKeyDown(_jumpButton)) { Jump(); }
        if (Input.GetKeyDown(_dashButton)) { StartDash(); } // вызов метода для подготовки к Рывку
    }


    private void FixedUpdate()
    {
        CheckGround();
        if (_onGround == false && _rb.velocity.y <= 0) { _anim.SetBool("onFall", true); } 
        else { _anim.SetBool("onFall", false); }

        if (_onDash == true) { Dash(); } // вызов метода Рывка
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

            _facingDirection *= -1; // довольно важно для Рывка
        }
    }

    private void HeroMove()
    {
        _rb.velocity = new Vector2(_inputPlayer.x * _moveSpeed, _rb.velocity.y);
        _anim.SetInteger("moveX", (int)Mathf.Abs(_inputPlayer.x));
    }



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckObj.position, groundCheckBoxSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _dashDistance); // отрисовка дичтанции Рывка
    }



    private float _playerGravityDefault; // для сохранения множителя гравитации перед рывком

    private bool _onDash = false; // процесс Рывка

    private Vector2 _dashCurrentPosition; // РАСЧИТАННАЯ следующая позиция при рывке
    private Vector2 _dashFinishPosition; // позиция в которой должен оказаться перс после Рывка

    [SerializeField] float _dashDistance = 3f; // дистанция Рывка
    [SerializeField] float _dashSpeed = 3f; // скорость Рывка

    private float _dashProgress = 0f; // текущий процент выполнения Рывка

    [SerializeField] private float _dashTimeCooldown = 3f; // время для перезарядки навыка "Рывок"
    private bool _dashReloaded = true; // готовность выполнить рывок (выполнена перезарядка)


    private void StartDash() // настройки ПЕРЕД Рывком
    {
        if (_dashReloaded == false) { return; } // выход, если перезарядка ещё не произошла

        _dashCurrentPosition = transform.position;

        // если НЕТ ВВОДА от игрока - движемся в направлении взгляда персонажа
        if (_inputPlayer == Vector2.zero) { _dashFinishPosition = _dashCurrentPosition + _dashDistance * Vector2.right * _facingDirection; }
        // если ввод есть, то движемся по направлению ввода
        else { _dashFinishPosition = _dashCurrentPosition + _dashDistance * _inputPlayer.normalized; }
        
        _dashProgress = 0f; // сбрасываем прогресс от предыдущего рывка

        _rb.gravityScale = 0; // отключаем гравитацию ( ОПЦИОНАЛЬНО! )
        _rb.velocity = Vector2.zero; // обнуляем возможные возможные скорости

        _dashReloaded = false; // ставим навык на перезарядку
        StartCoroutine(DashReloader()); // запускаем таймер для перезарядки Рывка

        _onDash = true; // за неимением машины состояний, обозначаем процесс Рывка
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        StopDash(); // при коллизиях останавливаем Рывок ( ОПЦИОНАЛЬНО! )
    }


    private void StopDash() // остановка Рывка при коллизиях (например, когда перс упёрся в стену)
    {
        // обнуляем скорость, возвращаем гравитацию, и прерываем выполнение Рывка
        _rb.velocity = Vector2.zero;
        _rb.gravityScale = _playerGravityDefault;
        _onDash = false;
    }


    IEnumerator DashReloader() // тупо "таймер перезарядки" Рывка
    {
        yield return new WaitForSeconds(_dashTimeCooldown);
        _dashReloaded = true;
    }


    private void Dash() // метод рывка выполняющийся каждый FixedUpdate
    {
        _dashProgress += Time.fixedDeltaTime * _dashSpeed / _dashDistance; // рассчёт прогресса выполнения Рывка за каждый FixedUpdate

        if (_dashProgress <= 1f) // если Рывок выполнен не на 100%, то двигаем перса, иначе - останавливаем Рывок
        {
            // ВЫСЧИТЫВАЕМ куда должен переместиться персонаж при текущем апдейте
            _dashCurrentPosition = Vector2.MoveTowards(transform.position, _dashFinishPosition, Time.fixedDeltaTime * _dashSpeed);
            _rb.MovePosition(_dashCurrentPosition); // двигаем перса на вычисленную позицию
            _anim.Play("Dash", 0, _dashProgress); // управляем анимацие Рывка ( ОПЦИОНАЛЬНО! )
            // пример: если анимация состоит из 10 кадров, а текущий процесс == 30%, то воспроизведётся 3-й кадр
            // если процесс будет равен 70% - 7-й.
        }
        else
        {
            StopDash();
        }
    }

}