using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieCqb
{
    public struct PlayerInputFrame
    {
        public Vector2 move; //이동 방향 입력
        public Vector2 pointer; //마우스의 화면 위치
        public bool interact; //문 또는 출구 상호작용
        public bool infect; //접촉 감염 유지 입력
        public bool liquid; //액체 감염 발동
        public bool droplet; //비말 감염 발동
        public bool stealth; //은신 발동
        public bool dash; //돌진 발동
        public bool command; //좀비 조종 발동
    }

    public sealed class PlayerInputComponent : MonoBehaviour
    {
        public PlayerInputFrame read() //현재 프레임의 키보드와 마우스 입력 읽기
        {
            Keyboard keyboard = Keyboard.current; //현재 키보드
            if (keyboard == null)
                return default;

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) //가로 이동 입력
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) //세로 이동 입력
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new PlayerInputFrame
            {
                move = Vector2.ClampMagnitude(new Vector2(x, y), 1f),
                pointer = Mouse.current?.position.ReadValue() ?? Vector2.zero,
                interact = keyboard.eKey.wasPressedThisFrame,
                infect = keyboard.spaceKey.isPressed,
                liquid = keyboard.qKey.wasPressedThisFrame,
                droplet = keyboard.rKey.wasPressedThisFrame,
                stealth = keyboard.fKey.wasPressedThisFrame,
                dash = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame,
                command = keyboard.cKey.wasPressedThisFrame
            };
        }
    }
}
