#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Threading;
using System.Windows.Forms;
using OpenTK.Input;
using Vocaluxe.Base;
using VocaluxeLib;

namespace Vocaluxe.Lib.Input
{
    class CGamePad : CControllerFramework
    {
        private int _GamePadIndex;
        private const float _LimitFactor = 1.0f;

        private GamePadState _OldButtonStates;

        private bool _Connected
        {
            get { return _GamePadIndex != -1; }
        }

        private Thread _HandlerThread;
        private AutoResetEvent _EvTerminate;
        private Object _Sync;
        private bool _Active;
        private CRumbleTimer _RumbleTimer;

        private int _repeat;
        private int _repeatTrigger;

        public override string GetName()
        {
            return "GamePad";
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            _Sync = new Object();
            _RumbleTimer = new CRumbleTimer();

            _HandlerThread = new Thread(_MainLoop) { Name = "GamePad", Priority = ThreadPriority.BelowNormal };
            _EvTerminate = new AutoResetEvent(false);

            _OldButtonStates = new GamePadState();
            return true;
        }

        public override void Close()
        {
            _Active = false;
            if (_HandlerThread != null)
            {
                //Join before freeing stuff
                //This also ensures, that no other thread is created till the current one is terminated
                _EvTerminate.Set();
                _HandlerThread.Join();
                _HandlerThread = null;
                _EvTerminate.Dispose();
                _EvTerminate = null;
            }
            base.Close();
        }

        public override void Connect()
        {
            if (_Active || _HandlerThread == null)
                return;
            _Active = true;
            _HandlerThread.Start();
        }

        public override void Disconnect()
        {
            Close();
        }

        public override bool IsConnected()
        {
            return _Connected;
        }

        public override void SetRumble(float duration)
        {
            lock (_Sync)
            {
                _RumbleTimer.Set(duration);
            }
        }

        private void _MainLoop()
        {

            while (_Active)
            {
                Thread.Sleep(5);

                if (!_Connected)
                {
                    if (!_DoConnect())
                        _EvTerminate.WaitOne(1000);
                }
                else
                {
                    bool startRumble;
                    bool stopRumble;
                    lock (_Sync)
                    {
                        startRumble = _RumbleTimer.ShouldStart;
                        stopRumble = _RumbleTimer.ShouldStop;
                    }

                    if (startRumble)
                        GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
                    else if (stopRumble)
                        GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);

                    _HandleButtons(GamePad.GetState(_GamePadIndex));
                }
            }

            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);

            _GamePadIndex = -1;
        }

        private void _HandleButtons(GamePadState buttonStates)
        {
            var key = Keys.None;

            if ((buttonStates.DPad.IsDown && !_OldButtonStates.DPad.IsDown) || (buttonStates.ThumbSticks.Left.Y < -0.8f && _OldButtonStates.ThumbSticks.Left.Y > -0.8f))
            {
                key = Keys.Down;
                _repeat = 0;
                _repeatTrigger = 80;
            }
            else if ((buttonStates.DPad.IsDown && _OldButtonStates.DPad.IsDown) || (buttonStates.ThumbSticks.Left.Y < -0.8f && _OldButtonStates.ThumbSticks.Left.Y < -0.8f))
            {
                _repeat++;
                if (_repeat >= _repeatTrigger)
                {
                    key = Keys.Down;
                    _repeat = 0;
                    _repeatTrigger = 15;
                }
            }
            else if ((buttonStates.DPad.IsUp && !_OldButtonStates.DPad.IsUp) || (buttonStates.ThumbSticks.Left.Y > 0.8f && _OldButtonStates.ThumbSticks.Left.Y < 0.8f))
            {
                key = Keys.Up;
                _repeat = 0;
                _repeatTrigger = 80;
            }
            else if ((buttonStates.DPad.IsUp && _OldButtonStates.DPad.IsUp) || (buttonStates.ThumbSticks.Left.Y > 0.8f && _OldButtonStates.ThumbSticks.Left.Y > 0.8f))
            {
                _repeat++;
                if (_repeat >= _repeatTrigger)
                {
                    key = Keys.Up;
                    _repeat = 0;
                    _repeatTrigger = 15;
                }
            }
            else if ((buttonStates.DPad.IsLeft && !_OldButtonStates.DPad.IsLeft) || (buttonStates.ThumbSticks.Left.X < -0.8f && _OldButtonStates.ThumbSticks.Left.X > -0.8f))
            {
                key = Keys.Left;
                _repeat = 0;
                _repeatTrigger = 80;
            }
            else if ((buttonStates.DPad.IsLeft && _OldButtonStates.DPad.IsLeft) || (buttonStates.ThumbSticks.Left.X < -0.8f && _OldButtonStates.ThumbSticks.Left.X < -0.8f))
            {
                _repeat++;
                if (_repeat >= _repeatTrigger)
                {
                    key = Keys.Left;
                    _repeat = 0;
                    _repeatTrigger = 15;
                }
            }
            else if ((buttonStates.DPad.IsRight && !_OldButtonStates.DPad.IsRight) || (buttonStates.ThumbSticks.Left.X > 0.8f && _OldButtonStates.ThumbSticks.Left.X < 0.8f))
            {
                key = Keys.Right;
                _repeat = 0;
                _repeatTrigger = 80;
            }
            else if ((buttonStates.DPad.IsRight && _OldButtonStates.DPad.IsRight) || (buttonStates.ThumbSticks.Left.X > 0.8f && _OldButtonStates.ThumbSticks.Left.X > 0.8f))
            {
                _repeat++;
                if (_repeat >= _repeatTrigger)
                {
                    key = Keys.Right;
                    _repeat = 0;
                    _repeatTrigger = 15;
                }
            }
            else if (buttonStates.Buttons.Start == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.Start == OpenTK.Input.ButtonState.Released)
                key = Keys.Space;
            else if (buttonStates.Buttons.A == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.A == OpenTK.Input.ButtonState.Released)
                key = Keys.Enter;
            else if (buttonStates.Buttons.B == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.B == OpenTK.Input.ButtonState.Released)
                key = Keys.Escape;
            else if (buttonStates.Buttons.X == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.X == OpenTK.Input.ButtonState.Released)
                key = Keys.F21;
            else if (buttonStates.Buttons.Y == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.Y == OpenTK.Input.ButtonState.Released)
                key = Keys.F22;
            else if (buttonStates.Buttons.LeftShoulder == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.LeftShoulder == OpenTK.Input.ButtonState.Released)
                key = Keys.F23;
            else if (buttonStates.Buttons.RightShoulder == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.RightShoulder == OpenTK.Input.ButtonState.Released)
                key = Keys.F24;
            else if (buttonStates.Buttons.Back == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.Back == OpenTK.Input.ButtonState.Released)
                key = Keys.Back;
            else if (buttonStates.Triggers.Left >= 0.8 && _OldButtonStates.Triggers.Left < 0.8)
                key = Keys.PageUp;
            else if (buttonStates.Triggers.Right >= 0.8 && _OldButtonStates.Triggers.Right < 0.8)
                key = Keys.PageDown;

            if (key != Keys.None)
                AddKeyEvent(new SKeyEvent(ESender.Gamepad, false, false, false, false, char.MinValue, key));

            _OldButtonStates = buttonStates;
        }

        private bool _DoConnect()
        {
            _GamePadIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (GamePad.GetCapabilities(i).IsConnected)
                {
                    _GamePadIndex = i;
                    break;
                }
            }


            GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);


            return _GamePadIndex != -1;
        }


    }
}