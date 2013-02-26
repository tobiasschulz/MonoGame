using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX.XInput;
using GBF = SharpDX.XInput.GamepadButtonFlags;

namespace Microsoft.Xna.Framework.Input
{
    public static class GamePad
    {
        public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
        {
            var controller = GetController(playerIndex);
            if (!controller.IsConnected)
                return new GamePadCapabilities(); // GamePadCapabilities.IsConnected = false by default

            var capabilities = controller.GetCapabilities(SharpDX.XInput.DeviceQueryType.Any);
            var ret = new GamePadCapabilities();
            switch (capabilities.SubType)
            {
                case DeviceSubType.ArcadeStick:
                    ret.GamePadType = GamePadType.ArcadeStick;
                    break;
                case DeviceSubType.DancePad:
                    ret.GamePadType = GamePadType.DancePad;
                    break;
                case DeviceSubType.DrumKit:
                    ret.GamePadType = GamePadType.DrumKit;
                    break;
                case DeviceSubType.Gamepad:
                    ret.GamePadType = GamePadType.GamePad;
                    break;
                case DeviceSubType.Guitar:
                    ret.GamePadType = GamePadType.Guitar;
                    break;
                case DeviceSubType.Wheel:
                    ret.GamePadType = GamePadType.Wheel;
                    break;
                default:
                    Debug.WriteLine("unexpected XInput DeviceSubType: {0}", capabilities.SubType.ToString());
                    ret.GamePadType = GamePadType.Unknown;
                    break;
            }

            var gamepad = capabilities.Gamepad;

            // digital buttons
            var buttons = gamepad.Buttons;
            ret.HasAButton = buttons.HasFlag(GBF.A);
            ret.HasBackButton = buttons.HasFlag(GBF.Back);
            ret.HasBButton = buttons.HasFlag(GBF.B);
            ret.HasBigButton = false; // TODO: what IS this? Is it related to GamePadType.BigGamePad?
            ret.HasDPadDownButton = buttons.HasFlag(GBF.DPadDown);
            ret.HasDPadLeftButton = buttons.HasFlag(GBF.DPadLeft);
            ret.HasDPadRightButton = buttons.HasFlag(GBF.DPadLeft);
            ret.HasDPadUpButton = buttons.HasFlag(GBF.DPadUp);
            ret.HasLeftShoulderButton = buttons.HasFlag(GBF.LeftShoulder);
            ret.HasLeftStickButton = buttons.HasFlag(GBF.LeftThumb);
            ret.HasRightShoulderButton = buttons.HasFlag(GBF.RightShoulder);
            ret.HasRightStickButton = buttons.HasFlag(GBF.RightThumb);
            ret.HasStartButton = buttons.HasFlag(GBF.Start);
            ret.HasXButton = buttons.HasFlag(GBF.X);
            ret.HasYButton = buttons.HasFlag(GBF.Y);

            // analog controls
            ret.HasRightTrigger = gamepad.LeftTrigger > 0;
            ret.HasRightXThumbStick = gamepad.RightThumbX != 0;
            ret.HasRightYThumbStick = gamepad.RightThumbY != 0;
            ret.HasLeftTrigger = gamepad.LeftTrigger > 0;
            ret.HasLeftXThumbStick = gamepad.LeftThumbX != 0;
            ret.HasLeftYThumbStick = gamepad.LeftThumbY != 0;

            // vibration
            ret.HasLeftVibrationMotor = capabilities.Vibration.LeftMotorSpeed > 0;
            ret.HasRightVibrationMotor = capabilities.Vibration.RightMotorSpeed > 0;

            // other
            ret.IsConnected = controller.IsConnected;
            ret.HasVoiceSupport = capabilities.Flags.HasFlag(CapabilityFlags.VoiceSupported);

            return ret;
        }

        private static Controller playerOne = new Controller(UserIndex.One);
        private static Controller playerTwo = new Controller(UserIndex.Two);
        private static Controller playerThree = new Controller(UserIndex.Three);
        private static Controller playerFour = new Controller(UserIndex.Four);
        private static Controller playerAny = new Controller(UserIndex.Any);

        public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode = GamePadDeadZone.IndependentAxes)
        {
            var controller = GetController(playerIndex);
            if (!controller.IsConnected)
                return new GamePadState(); // GamePadState.IsConnected = false by default

            var gamepad = controller.GetState().Gamepad;

            var thumbSticks = new GamePadThumbSticks(
                ConvertThumbStick(gamepad.LeftThumbX, gamepad.LeftThumbY, Gamepad.LeftThumbDeadZone, deadZoneMode),
                ConvertThumbStick(gamepad.RightThumbX, gamepad.RightThumbY, Gamepad.RightThumbDeadZone, deadZoneMode));

            var triggers = new GamePadTriggers(
                gamepad.LeftTrigger / (float)byte.MaxValue, 
                gamepad.RightTrigger / (float)byte.MaxValue);

            var state = new GamePadState(
                thumbSticks: thumbSticks,
                triggers: triggers,
                buttons: ConvertToButtons(
                    buttonFlags: gamepad.Buttons,
                    leftThumbX: gamepad.LeftThumbX,
                    leftThumbY: gamepad.LeftThumbY,
                    rightThumbX: gamepad.RightThumbX,
                    rightThumbY: gamepad.RightThumbY,
                    leftTrigger: gamepad.LeftTrigger,
                    rightTrigger: gamepad.RightTrigger),
                dPad: ConvertToGamePadDPad(gamepad.Buttons));

            return state;
        }

        public static void SetVibration(PlayerIndex playerIndex, float leftMotorSpeed, float rightMotorSpeed)
        {
            var controller = GetController(playerIndex);
            if (controller.IsConnected)
                controller.SetVibration(new Vibration
                {
                    LeftMotorSpeed = (short)(leftMotorSpeed * 65535),
                    RightMotorSpeed = (short)(rightMotorSpeed * 65535)
                });
        }

        private static Controller GetController(PlayerIndex playerIndex)
        {
            Controller controller = null;
            switch (playerIndex)
            {
                case PlayerIndex.One:
                    // TODO: need to research XInput vs. XNA behavior with regards to which player controllers
                    // are assigned to in XInput, and if they are reassigned as controllers are added/removed.
                    // for now, we won't use playerAny unless you pass (PlayerIndex)0
                    //controller = !playerOne.IsConnected && playerAny.IsConnected ? playerAny : playerOne;
                    controller = playerOne;
                    break;
                case PlayerIndex.Two:
                    controller = playerTwo;
                    break;
                case PlayerIndex.Three:
                    controller = playerThree;
                    break;
                case PlayerIndex.Four:
                    controller = playerFour;
                    break;
                default:
                    controller = playerAny;
                    break;
            }
            return controller;
        }

        private static Vector2 ConvertThumbStick(short x, short y, short deadZone, GamePadDeadZone deadZoneMode)
        {
            if (deadZoneMode == GamePadDeadZone.IndependentAxes)
            {
                // using int to prevent overrun
                int fx = x;
                int fy = y;
                int fdz = deadZone;
                if (fx * fx < fdz * fdz)
                    x = 0;
                if (fy * fy < fdz * fdz)
                    y = 0;
            }
            else if (deadZoneMode == GamePadDeadZone.Circular)
            {
                // using int to prevent overrun
                int fx = x;
                int fy = y;
                int fdz = deadZone;
                if ((fx * fx) + (fy * fy) < fdz * fdz)
                {
                    x = 0;
                    y = 0;
                }
            }

            return new Vector2(x < 0 ? -((float)x / (float)short.MinValue) : (float)x / (float)short.MaxValue,
                               y < 0 ? -((float)y / (float)short.MinValue) : (float)y / (float)short.MaxValue);
        }

        private static ButtonState ConvertToButtonState(GamepadButtonFlags buttonFlags, GamepadButtonFlags desiredButton)
        {
            return buttonFlags.HasFlag(desiredButton) ? ButtonState.Pressed : ButtonState.Released;
        }

        private static GamePadDPad ConvertToGamePadDPad(GamepadButtonFlags buttonFlags)
        {
            return new GamePadDPad(
                upValue: ConvertToButtonState(buttonFlags, GamepadButtonFlags.DPadUp),
                downValue: ConvertToButtonState(buttonFlags, GamepadButtonFlags.DPadDown),
                leftValue: ConvertToButtonState(buttonFlags, GamepadButtonFlags.DPadLeft),
                rightValue: ConvertToButtonState(buttonFlags, GamepadButtonFlags.DPadRight));
        }

        private static Buttons AddButtonIfPressed(Buttons originalButtonState,
            GamepadButtonFlags buttonFlags,
            GamepadButtonFlags xInputButton,
            Buttons xnaButton)
        {
            ButtonState buttonState = ConvertToButtonState(buttonFlags, xInputButton);
            return buttonState == ButtonState.Pressed ? originalButtonState | xnaButton : originalButtonState;
        }

        private static readonly List<Tuple<GBF, Buttons>> buttonMap = new List<Tuple<GBF, Buttons>>()
            {
                Tuple.Create(GBF.A, Buttons.A),
                Tuple.Create(GBF.B, Buttons.B),
                Tuple.Create(GBF.Back, Buttons.Back),
                Tuple.Create(GBF.DPadDown, Buttons.DPadDown),
                Tuple.Create(GBF.DPadLeft, Buttons.DPadLeft),
                Tuple.Create(GBF.DPadRight, Buttons.DPadRight),
                Tuple.Create(GBF.DPadUp, Buttons.DPadUp),
                Tuple.Create(GBF.LeftShoulder, Buttons.LeftShoulder),
                Tuple.Create(GBF.RightShoulder, Buttons.RightShoulder),
                Tuple.Create(GBF.LeftThumb, Buttons.LeftStick),
                Tuple.Create(GBF.RightThumb, Buttons.RightStick),
                Tuple.Create(GBF.Start, Buttons.Start),
                Tuple.Create(GBF.X, Buttons.X),
                Tuple.Create(GBF.Y, Buttons.Y),
            };

        private static Buttons AddThumbstickButtons(
            short thumbX, short thumbY, short deadZone,
            Buttons bitFieldToAddTo,
            Buttons thumbstickLeft,
            Buttons thumbStickRight,
            Buttons thumbStickUp,
            Buttons thumbStickDown)
        {
            // TODO: this needs adjustment. Very naive implementation. Doesn't match XNA yet
            if (thumbX < -deadZone)
                bitFieldToAddTo = bitFieldToAddTo | thumbstickLeft;
            if (thumbX > deadZone)
                bitFieldToAddTo = bitFieldToAddTo | thumbStickRight;
            if (thumbY < -deadZone)
                bitFieldToAddTo = bitFieldToAddTo | thumbStickUp;
            else if (thumbY > deadZone)
                bitFieldToAddTo = bitFieldToAddTo | thumbStickDown;
            return bitFieldToAddTo;
        }

        private static GamePadButtons ConvertToButtons(GamepadButtonFlags buttonFlags,
            short leftThumbX, short leftThumbY,
            short rightThumbX, short rightThumbY,
            byte leftTrigger,
            byte rightTrigger)
        {
            var ret = new Buttons();
            for (int i = 0; i < buttonMap.Count; i++)
            {
                var curMap = buttonMap[i];
                ret = AddButtonIfPressed(ret, buttonFlags, curMap.Item1, curMap.Item2);
            }

            ret = AddThumbstickButtons(leftThumbX, leftThumbY,
                Gamepad.LeftThumbDeadZone, ret,
                Buttons.LeftThumbstickLeft, Buttons.LeftThumbstickRight,
                Buttons.LeftThumbstickUp, Buttons.LeftThumbstickDown);

            ret = AddThumbstickButtons(rightThumbX, rightThumbY,
                Gamepad.RightThumbDeadZone, ret,
                Buttons.RightThumbstickLeft, Buttons.RightThumbstickRight,
                Buttons.RightThumbstickUp, Buttons.RightThumbstickDown);

            if (leftTrigger >= Gamepad.TriggerThreshold)
                ret = ret | Buttons.LeftTrigger;

            if (rightTrigger >= Gamepad.TriggerThreshold)
                ret = ret | Buttons.RightTrigger;

            var r = new GamePadButtons(ret);
            return r;
        }
    }
}
