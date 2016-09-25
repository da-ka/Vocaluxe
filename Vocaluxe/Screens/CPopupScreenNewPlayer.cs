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
using System.Collections.Generic;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;

namespace Vocaluxe.Screens
{

    public class CPopupScreenNewPlayer : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideProfiles = "SelectSlideProfiles";
        private const string _SelectSlideDifficulty = "SelectSlideDifficulty";
        private const string _SelectSlideAvatars = "SelectSlideAvatars";
        private const string _SelectSlideUserRole = "SelectSlideUserRole";
        private const string _SelectSlideActive = "SelectSlideActive";
        private const string _ButtonPlayerName = "ButtonPlayerName";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonSave = "ButtonSave";
        private const string _ButtonNew = "ButtonNew";
        private const string _ButtonDelete = "ButtonDelete";

        private int _NewProfileID;
        private const string _StaticAvatar = "StaticAvatar";
        private bool _ProfilesChanged;
        private bool _AvatarsChanged;

        private EEditMode _EditMode;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[]
                {_ButtonPlayerName, _ButtonExit, _ButtonSave, _ButtonNew, _ButtonDelete};
            _ThemeSelectSlides = new string[] { _SelectSlideProfiles, _SelectSlideDifficulty, _SelectSlideAvatars, _SelectSlideUserRole, _SelectSlideActive };
            _ThemeStatics = new string[] { _StaticAvatar };

            _EditMode = EEditMode.None;
            _ProfilesChanged = false;
            _AvatarsChanged = false;
            CProfiles.AddProfileChangedCallback(_OnProfileChanged);
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _SelectSlides[_SelectSlideDifficulty].SetValues<EGameDifficulty>(0);
            _SelectSlides[_SelectSlideUserRole].SetValues<EUserRole>(0);
            _SelectSlides[_SelectSlideActive].SetValues<EOffOn>(0);
            _Statics[_StaticAvatar].Aspect = EAspect.Crop;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode))
            {
                switch (_EditMode)
                {
                    case EEditMode.None:
                        break;
                    case EEditMode.PlayerName:
                        CProfiles.AddGetPlayerName(_NewProfileID, keyEvent.Unicode);
                        _ProfilesChanged = true;
                        break;
                }
            }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                        if (_EditMode == EEditMode.PlayerName)
                            _EditMode = EEditMode.None;
                        else
                            CGraphics.HidePopup(EPopupScreens.PopupNewPlayer);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                            CGraphics.HidePopup(EPopupScreens.PopupNewPlayer);
                        else if (_Buttons[_ButtonSave].Selected)
                            _SaveProfiles();
                        else if (_Buttons[_ButtonNew].Selected)
                            _NewProfile();
                        else if (_Buttons[_ButtonPlayerName].Selected)
                        {
                            if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                                _EditMode = EEditMode.PlayerName;
                            else
                                _EditMode = EEditMode.None;
                        }
                        else if (_Buttons[_ButtonDelete].Selected)
                            _DeleteProfile();
                        break;

                    case Keys.Back:
                        if (_EditMode == EEditMode.PlayerName)
                        {
                            CProfiles.GetDeleteCharInPlayerName(_NewProfileID);
                            _ProfilesChanged = true;
                        }
                        else
                            CGraphics.HidePopup(EPopupScreens.PopupNewPlayer);
                        break;

                    case Keys.Delete:
                        _DeleteProfile();
                        break;
                }
                if (_SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(_NewProfileID,
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_NewProfileID,
                                        _SelectSlides[_SelectSlideAvatars].SelectedTag);
                }
                else if (_SelectSlides[_SelectSlideUserRole].Selected)
                {
                    CProfiles.SetUserRoleProfile(_NewProfileID,
                                                 (EUserRole)_SelectSlides[_SelectSlideUserRole].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_NewProfileID,
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_EditMode == EEditMode.None)
                base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.HidePopup(EPopupScreens.PopupNewPlayer);
                else if (_Buttons[_ButtonSave].Selected)
                    _SaveProfiles();
                else if (_Buttons[_ButtonNew].Selected)
                    _NewProfile();
                else if (_Buttons[_ButtonDelete].Selected)
                    _DeleteProfile();
                else if (_Buttons[_ButtonPlayerName].Selected)
                {
                    if (CProfiles.NumProfiles > 0 && _EditMode != EEditMode.PlayerName)
                        _EditMode = EEditMode.PlayerName;
                    else
                        _EditMode = EEditMode.None;
                }
                else if (_SelectSlides[_SelectSlideDifficulty].Selected)
                {
                    CProfiles.SetDifficulty(_NewProfileID,
                                            (EGameDifficulty)_SelectSlides[_SelectSlideDifficulty].Selection);
                }
                else if (_SelectSlides[_SelectSlideAvatars].Selected)
                {
                    CProfiles.SetAvatar(_NewProfileID,
                                        _SelectSlides[_SelectSlideAvatars].SelectedTag);
                }
                else if (_SelectSlides[_SelectSlideUserRole].Selected)
                {
                    CProfiles.SetUserRoleProfile(_NewProfileID,
                                                 (EUserRole)_SelectSlides[_SelectSlideUserRole].Selection);
                }
                else if (_SelectSlides[_SelectSlideActive].Selected)
                {
                    CProfiles.SetActive(_NewProfileID,
                                        (EOffOn)_SelectSlides[_SelectSlideActive].Selection);
                }
            }

            if (mouseEvent.RB)
                CGraphics.HidePopup(EPopupScreens.PopupNewPlayer);
            return true;
        }

        public override bool UpdateGame()
        {
            if (_AvatarsChanged)
                _LoadAvatars(true);

            if (_ProfilesChanged)
                _LoadProfiles(true);

            if (_SelectSlides[_SelectSlideProfiles].Selection > -1)
            {
                _Buttons[_ButtonPlayerName].Text.Text = CProfiles.GetPlayerName(_NewProfileID);
                if (_EditMode == EEditMode.PlayerName)
                    _Buttons[_ButtonPlayerName].Text.Text += "|";

                _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(_NewProfileID);
                _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(_NewProfileID);
                _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(_NewProfileID);

                int avatarID = CProfiles.GetAvatarID(_NewProfileID);
                _SelectSlides[_SelectSlideAvatars].SelectedTag = avatarID;
                _Statics[_StaticAvatar].Texture = CProfiles.GetAvatarTexture(avatarID);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _LoadAvatars(false);
            _LoadProfiles(false);
            UpdateGame();
            _NewProfile();
        }

        public override void OnClose()
        {
            base.OnClose();
            _EditMode = EEditMode.None;
        }

        private void _OnProfileChanged(EProfileChangedFlags flags)
        {
            if (EProfileChangedFlags.Avatar == (EProfileChangedFlags.Avatar & flags))
                _AvatarsChanged = true;

            if (EProfileChangedFlags.Profile == (EProfileChangedFlags.Profile & flags))
                _ProfilesChanged = true;
        }

        private void _ClosePopup()
        {
            string _NewProfileName = CProfiles.GetPlayerName(_NewProfileID);
            if(_NewProfileName.Length < 1)
            {
                _DeleteProfile();
            }
            CGraphics.HidePopup(EPopupScreens.PopupNewPlayer);
        }

        private void _NewProfile()
        {
            _EditMode = EEditMode.None;
            int id = CProfiles.NewProfile();
            //_LoadProfiles(false);
            _NewProfileID = id;

            CProfiles.SetAvatar(_NewProfileID, _SelectSlides[_SelectSlideAvatars].SelectedTag);

            _SelectElement(_Buttons[_ButtonPlayerName]);
            _EditMode = EEditMode.PlayerName;
        }

        private void _SaveProfiles()
        {
            _EditMode = EEditMode.None;
            CProfiles.SaveProfiles();
        }

        private void _DeleteProfile()
        {
            _EditMode = EEditMode.None;

            CProfiles.DeleteProfile(_NewProfileID);
        }

        private void _LoadProfiles(bool keep)
        {
            string name = String.Empty;
            if (_EditMode == EEditMode.PlayerName)
                name = CProfiles.GetPlayerName(_SelectSlides[_SelectSlideProfiles].SelectedTag);

            int selectedProfileID = _SelectSlides[_SelectSlideProfiles].SelectedTag;
            _SelectSlides[_SelectSlideProfiles].Clear();

            CProfile[] profiles = CProfiles.GetProfiles();
            foreach (CProfile profile in profiles)
                _SelectSlides[_SelectSlideProfiles].AddValue(profile.PlayerName, null, profile.ID);

            if (CProfiles.NumProfiles > 0 && CProfiles.NumAvatars > 0)
            {
                if (selectedProfileID != -1)
                    _SelectSlides[_SelectSlideProfiles].SelectedTag = selectedProfileID;
                else
                {
                    _SelectSlides[_SelectSlideProfiles].Selection = 0;
                    selectedProfileID = _SelectSlides[_SelectSlideProfiles].SelectedTag;
                }

                if (!keep)
                {
                    _SelectSlides[_SelectSlideDifficulty].Selection = (int)CProfiles.GetDifficulty(selectedProfileID);
                    _SelectSlides[_SelectSlideUserRole].Selection = (int)CProfiles.GetUserRoleProfile(selectedProfileID);
                    _SelectSlides[_SelectSlideActive].Selection = (int)CProfiles.GetActive(selectedProfileID);
                    _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarID(selectedProfileID);
                }

                if (_EditMode == EEditMode.PlayerName)
                    CProfiles.SetPlayerName(_SelectSlides[_SelectSlideProfiles].SelectedTag, name);
            }
            _ProfilesChanged = false;
        }

        private void _LoadAvatars(bool keep)
        {
            int selectedAvatarID = _SelectSlides[_SelectSlideAvatars].SelectedTag;
            _SelectSlides[_SelectSlideAvatars].Clear();
            IEnumerable<CAvatar> avatars = CProfiles.GetAvatars();
            if (avatars != null)
            {
                foreach (CAvatar avatar in avatars)
                    _SelectSlides[_SelectSlideAvatars].AddValue(avatar.GetDisplayName(), null, avatar.ID);
            }

            if (keep)
            {
                _SelectSlides[_SelectSlideAvatars].SelectedTag = selectedAvatarID;
                CProfiles.SetAvatar(_SelectSlides[_SelectSlideProfiles].SelectedTag, selectedAvatarID);
            }
            else
                _SelectSlides[_SelectSlideAvatars].SelectedTag = CProfiles.GetAvatarID(_SelectSlides[_SelectSlideProfiles].SelectedTag);

            _AvatarsChanged = false;
        }
    }
}