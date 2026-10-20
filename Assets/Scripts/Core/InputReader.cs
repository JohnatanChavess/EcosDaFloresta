using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Centraliza a leitura dos controles (teclado e gamepad) usando o Input Manager clássico.
    /// Também permite simular entradas no modo de autoteste.
    /// </summary>
    public static class InputReader
    {
        public static bool Simulated;
        public static float SimHorizontal;
        public static float SimVertical;
        public static bool SimJumpPressed;
        public static bool SimJumpHeld;
        public static bool SimRun;

        public static float Horizontal
        {
            get
            {
                if (Simulated) return SimHorizontal;
                float h = Input.GetAxisRaw("Horizontal");
                return Mathf.Abs(h) < 0.2f ? 0f : Mathf.Sign(h);
            }
        }

        public static float Vertical
        {
            get
            {
                if (Simulated) return SimVertical;
                float v = Input.GetAxisRaw("Vertical");
                return Mathf.Abs(v) < 0.3f ? 0f : Mathf.Sign(v);
            }
        }

        public static bool JumpPressed =>
            Simulated ? SimJumpPressed :
            Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.K);

        public static bool JumpHeld =>
            Simulated ? SimJumpHeld :
            Input.GetButton("Jump") || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.K);

        public static bool RunHeld =>
            Simulated ? SimRun :
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.JoystickButton2);

        public static bool PausePressed =>
            Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.JoystickButton7);

        public static bool MutePressed => Input.GetKeyDown(KeyCode.M);
    }
}
