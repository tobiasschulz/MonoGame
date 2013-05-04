#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

using SDL2;

namespace Microsoft.Xna.Framework.Input
{
    //
    // Summary:
    //     Allows retrieval of user interaction with an Xbox 360 Controller and setting
    //     of controller vibration motors. Reference page contains links to related
    //     code samples.
    public static class GamePad
    {
        // NOTE: GamePad uses SDL_GameController, SdlGamePad uses SDL_Joystick -flibit

        // Is this thing even running?
        private static bool enabled = false;

        // The SDL device lists
        private static IntPtr[] INTERNAL_devices = new IntPtr[4];
        private static IntPtr[] INTERNAL_haptics = new IntPtr[4];
        
        // Explicitly initialize the SDL Joystick/GameController subsystems
        private static bool Init()
        {
            return SDL.SDL_InitSubSystem(SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_GAMECONTROLLER) == 0;
        }
        
        // Call this when you're done, if you don't want to depend on SDL_Quit();
        internal static void Cleanup()
        {
            if (SDL.SDL_WasInit(SDL.SDL_INIT_GAMECONTROLLER) == 1)
            {
                SDL.SDL_QuitSubSystem(SDL.SDL_INIT_GAMECONTROLLER);
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_JOYSTICK) == 1)
            {
                SDL.SDL_QuitSubSystem(SDL.SDL_INIT_JOYSTICK);
            }
        }
        
        // Convenience method to check for Rumble support
        private static bool INTERNAL_HapticSupported(PlayerIndex playerIndex)
        {
            return !(   INTERNAL_haptics[(int)playerIndex] == IntPtr.Zero ||
                        SDL.SDL_HapticRumbleSupported(INTERNAL_haptics[(int)playerIndex]) == 0  );
        }

        // Prepare the MonoGameJoystick configuration system
        private static void INTERNAL_AutoConfig()
        {
            if (!Init())
            {
                return;
            }

#if DEBUG
			Console.WriteLine("Number of joysticks: " + SDL.SDL_NumJoysticks());
#endif
            // Limit to the first 4 sticks to avoid crashes.
            int numSticks = Math.Min(4, SDL.SDL_NumJoysticks());

            for (int x = 0; x < numSticks; x++)
            {
                // Initialize either a GameController or a Joystick.
                if (SDL.SDL_IsGameController(x) == SDL.SDL_bool.SDL_TRUE)
                {
                    INTERNAL_devices[x] = SDL.SDL_GameControllerOpen(x);
                    // Initialize the haptics for each joystick.
                    INTERNAL_haptics[x] = SDL.SDL_HapticOpen(x);
                    if (INTERNAL_HapticSupported((PlayerIndex)x))
                    {
                        SDL.SDL_HapticRumbleInit(INTERNAL_haptics[x]);
                    }
                    System.Console.WriteLine(
                        "Controller " + x + ", " +
                        SDL.SDL_GameControllerName(INTERNAL_devices[x]) +
                        ", will use SDL_GameController support."
                    );
                }
            }

            // We made it!
            enabled = true;
        }
		
        // This is where we actually read in the controller input!
        private static GamePadState ReadState(PlayerIndex index, GamePadDeadZone deadZone)
        {
            IntPtr device = INTERNAL_devices[(int) index];
            if (device == IntPtr.Zero)
            {
                return GamePadState.InitializedState;
            }
            
            // Do not attempt to understand this number at all costs!
            const float DeadZoneSize = 0.27f;

                // Sticks
                GamePadThumbSticks gc_sticks = new GamePadThumbSticks(
                    new Vector2(
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX
                        ) / 32768.0f,
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY
                        ) / 32768.0f
                    ),
                    new Vector2(
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX
                        ) / 32768.0f,
                        (float) SDL.SDL_GameControllerGetAxis(
                            device,
                            SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY
                        ) / 32768.0f
                    )
                );
                gc_sticks.ApplyDeadZone(deadZone, DeadZoneSize);
                
                // Triggers
                GamePadTriggers gc_triggers = new GamePadTriggers(
                    (float) SDL.SDL_GameControllerGetAxis(device, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) / 32768.0f,
                    (float) SDL.SDL_GameControllerGetAxis(device, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) / 32768.0f
                );
                
                // Buttons
                GamePadButtons gc_buttons;
                Buttons gc_buttonState = (Buttons) 0;
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A) != 0)
                {
                    gc_buttonState |= Buttons.A;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B) != 0)
                {
                    gc_buttonState |= Buttons.B;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X) != 0)
                {
                    gc_buttonState |= Buttons.X;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y) != 0)
                {
                    gc_buttonState |= Buttons.Y;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) != 0)
                {
                    gc_buttonState |= Buttons.Back;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE) != 0)
                {
                    gc_buttonState |= Buttons.BigButton;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) != 0)
                {
                    gc_buttonState |= Buttons.Start;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK) != 0)
                {
                    gc_buttonState |= Buttons.LeftStick;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK) != 0)
                {
                    gc_buttonState |= Buttons.RightStick;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) != 0)
                {
                    gc_buttonState |= Buttons.LeftShoulder;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) != 0)
                {
                    gc_buttonState |= Buttons.RightShoulder;
                }
                gc_buttons = new GamePadButtons(gc_buttonState);
                
                // DPad
                GamePadDPad gc_dpad;
                Buttons gc_dpadState = (Buttons) 0;
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP) != 0)
                {
                    gc_dpadState |= Buttons.DPadUp;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN) != 0)
                {
                    gc_dpadState |= Buttons.DPadDown;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT) != 0)
                {
                    gc_dpadState |= Buttons.DPadLeft;
                }
                if (SDL.SDL_GameControllerGetButton(device, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) != 0)
                {
                    gc_dpadState |= Buttons.DPadRight;
                }
                gc_dpad = new GamePadDPad(gc_dpadState);
                
                return new GamePadState(
                    gc_sticks,
                    gc_triggers,
                    gc_buttons,
                    gc_dpad
                );
        }

        //
        // Summary:
        //     Retrieves the capabilities of an Xbox 360 Controller.
        //
        // Parameters:
        //   playerIndex:
        //     Index of the controller to query.
        public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
        {
            if (SDL.SDL_IsGameController((int) playerIndex) == SDL.SDL_bool.SDL_TRUE)
            {
                // An SDL_GameController will _always_ be feature-complete.
                return new GamePadCapabilities()
                {
                    IsConnected = INTERNAL_devices[(int) playerIndex] != IntPtr.Zero,
                    HasAButton = true,
                    HasBButton = true,
                    HasXButton = true,
                    HasYButton = true,
                    HasBackButton = true,
                    HasStartButton = true,
                    HasDPadDownButton = true,
                    HasDPadLeftButton = true,
                    HasDPadRightButton = true,
                    HasDPadUpButton = true,
                    HasLeftShoulderButton = true,
                    HasRightShoulderButton = true,
                    HasLeftStickButton = true,
                    HasRightStickButton = true,
                    HasLeftTrigger = true,
                    HasRightTrigger = true,
                    HasLeftXThumbStick = true,
                    HasLeftYThumbStick = true,
                    HasRightXThumbStick = true,
                    HasRightYThumbStick = true,
                    HasBigButton = true,
                    HasLeftVibrationMotor = INTERNAL_HapticSupported(playerIndex),
                    HasRightVibrationMotor = INTERNAL_HapticSupported(playerIndex),
                    HasVoiceSupport = false
                };
            }
            
            return new GamePadCapabilities();
        }
        
        //
        // Summary:
        //     Gets the current state of a game pad controller. Reference page contains
        //     links to related code samples.
        //
        // Parameters:
        //   playerIndex:
        //     Player index for the controller you want to query.
        public static GamePadState GetState(PlayerIndex playerIndex)
        {
            return GetState(playerIndex, GamePadDeadZone.IndependentAxes);
        }
        
        //
        // Summary:
        //     Gets the current state of a game pad controller, using a specified dead zone
        //     on analog stick positions. Reference page contains links to related code
        //     samples.
        //
        // Parameters:
        //   playerIndex:
        //     Player index for the controller you want to query.
        //
        //   deadZoneMode:
        //     Enumerated value that specifies what dead zone type to use.
        public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode)
        {
            if (!enabled)
            {
                INTERNAL_AutoConfig();
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_JOYSTICK) == 1)
            {
                SDL.SDL_JoystickUpdate();
            }
            if (SDL.SDL_WasInit(SDL.SDL_INIT_GAMECONTROLLER) == 1)
            {
                SDL.SDL_GameControllerUpdate();
            }
            return ReadState(playerIndex, deadZoneMode);
        }

        //
        // Summary:
        //     Sets the vibration motor speeds on an Xbox 360 Controller. Reference page
        //     contains links to related code samples.
        //
        // Parameters:
        //   playerIndex:
        //     Player index that identifies the controller to set.
        //
        //   leftMotor:
        //     The speed of the left motor, between 0.0 and 1.0. This motor is a low-frequency
        //     motor.
        //
        //   rightMotor:
        //     The speed of the right motor, between 0.0 and 1.0. This motor is a high-frequency
        //     motor.
        public static void SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
        {
            if (INTERNAL_HapticSupported(playerIndex))
            {
                //return false;
            }
            
            if (leftMotor <= 0.0f && rightMotor <= 0.0f)
            {
                SDL.SDL_HapticRumbleStop(INTERNAL_haptics[(int) playerIndex]);
            }
            else
            {
                // FIXME: Left and right motors as separate rumble?
                SDL.SDL_HapticRumblePlay(
                    INTERNAL_haptics[(int) playerIndex],
                    (leftMotor + rightMotor) / 2.0f,
                    uint.MaxValue // Oh my...
                );
            }
            //return true;
        }
    }
}