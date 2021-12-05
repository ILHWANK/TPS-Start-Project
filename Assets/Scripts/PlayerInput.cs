using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public string fireButtonName = "Fire1";
    public string jumpButtonName = "Jump";

    public string moveHorizontalAxisName = "Horizontal"; //좌우 수평 방향 움직임을 감지할 때 사용할 입력 축 이름 
    public string moveVerticalAxisName = "Vertical"; // 앞뒤 방향 입력을 감지할 때 사용할 입력 축 이름
    public string reloadButtonName = "Reload";

    public Vector2 moveInput { get; private set; }
    public bool fire { get; private set; }
    public bool reload { get; private set; }
    public bool jump { get; private set; }
    /* 
     * 감지된 입력 값을 지정 하기 위한 프로퍼티 선언
     * 입력을 받을 때에는 제약 없이 입력을 받아도 상관 없지만
     * Player 의 프로터티 값 을 변경하는 것을 안되기 때문에 
     * private 로 접근에 제약을 둠. 즉 외부에 값을 수정 할 수 없다.  
    */
    
    private void Update()
    {
        if (GameManager.Instance != null //GameManeger 는 싱글턴 패턴으로 구현 되어 있다.
            && GameManager.Instance.isGameover)
        {
            moveInput = Vector2.zero;
            fire = false;
            reload = false;
            jump = false;
            return;
        }

        moveInput = new Vector2(Input.GetAxis(moveHorizontalAxisName), Input.GetAxis(moveVerticalAxisName));
        if (moveInput.sqrMagnitude > 1) moveInput = moveInput.normalized;
        // 키보드에서 입력된 값은 1을 초과 할 수 있기때문에 1을 초과  한경우 해당 값을 1로 만들어 준다.
        // 그리고 입력된 값에 Speed 값을 곱하여 사용 함.
        // sqrMagnitude 을 사용하면 연산을 좀더 가볍게 할 수 있다. (루투의 연산 값이 빠진다.)

        jump = Input.GetButtonDown(jumpButtonName);
        fire = Input.GetButton(fireButtonName);
        reload = Input.GetButtonDown(reloadButtonName);
    }
}