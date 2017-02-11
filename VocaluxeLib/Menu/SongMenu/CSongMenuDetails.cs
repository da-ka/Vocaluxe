﻿#region license
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
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu.SongMenu
{
    class CSongMenuDetails : CSongMenuFramework
    {
        private SRectF _ScrollRect;
        private List<CStatic> _Tiles;
        private List<CText> _Artists;
        private List<CText> _Titles;
        private readonly CStatic _CoverBig;
        private readonly CStatic _TextBG;
        private readonly CStatic _DuetIcon;
        private readonly CStatic _VideoIcon;
        private readonly CStatic _MedleyCalcIcon;
        private readonly CStatic _MedleyTagIcon;

        private CTextureRef _CoverBigBGTexture;
        private CTextureRef _CoverBGTexture;

        private readonly CText _Artist;
        private readonly CText _Title;
        private readonly CText _SongLength;

        private float _SpaceW;
        private float _SpaceH;

        private int _TileW;
        private int _TileH;

        private int _ListLength;
        private float _ListTextWidth;

        // Offset is the song or categoryNr of the tile in the left upper corner
        private int _Offset;

        private float _Length = -1f;
        private int _LastKnownElements;
        private int _LastKnownCategory;

        private bool _MouseWasInRect;

        private readonly List<IMenuElement> _SubElements = new List<IMenuElement>();

        public override float SelectedTileZoomFactor
        {
            get { return 1.2f; }
        }

        protected override int _SelectionNr
        {
            set
            {
                int max = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
                base._SelectionNr = value.Clamp(-1, max - 1, true);
                //Update list in case we scrolled 
                _UpdateList();

                _UpdateTileSelection();
            }
        }

        public override bool SmallView
        {
            set
            {
                if (SmallView == value)
                    return;
                base.SmallView = value;
                _InitTiles();
                _UpdateList(true);
                _UpdateTileSelection();
            }
        }

        protected override int _PreviewNr
        {
            set
            {
                if (value == base._PreviewNr)
                {
                    if (!CBase.BackgroundMusic.IsPlaying() && value != -1)
                        CBase.BackgroundMusic.Play();
                    return;
                }
                base._PreviewNr = value;
                _UpdatePreview();
            }
        }

        public CSongMenuDetails(SThemeSongMenu theme, int partyModeID) : base(theme, partyModeID)
        {
            _ListLength = _Theme.SongMenuDetails.ListLength;
            _SpaceW = _Theme.SongMenuDetails.SpaceW;
            _SpaceH = _Theme.SongMenuDetails.SpaceH;
            _Artist = new CText(_Theme.SongMenuDetails.TextArtist, _PartyModeID);
            _Title = new CText(_Theme.SongMenuDetails.TextTitle, _PartyModeID);
            _SongLength = new CText(_Theme.SongMenuDetails.TextSongLength, _PartyModeID);
            _CoverBig = new CStatic(_Theme.SongMenuDetails.StaticCoverBig, _PartyModeID);
            _TextBG = new CStatic(_Theme.SongMenuDetails.StaticTextBG, _PartyModeID);
            _DuetIcon = new CStatic(_Theme.SongMenuDetails.StaticDuetIcon, _PartyModeID);
            _VideoIcon = new CStatic(_Theme.SongMenuDetails.StaticVideoIcon, _PartyModeID);
            _MedleyCalcIcon = new CStatic(_Theme.SongMenuDetails.StaticMedleyCalcIcon, _PartyModeID);
            _MedleyTagIcon = new CStatic(_Theme.SongMenuDetails.StaticMedleyTagIcon, _PartyModeID);
            _SubElements.AddRange(new IMenuElement[] {_Artist, _Title, _SongLength, _DuetIcon, _VideoIcon, _MedleyCalcIcon, _MedleyTagIcon});
        }

        public override bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            string item = xmlPath + "/" + elementName;

            bool themeLoaded = true;
            themeLoaded &= base.LoadTheme(xmlPath, elementName, xmlReader);


            themeLoaded &= _Artist.LoadTheme(item + "/SongMenuDetails", "TextArtist", xmlReader);
            themeLoaded &= _Title.LoadTheme(item + "/SongMenuDetails", "TextTitle", xmlReader);
            themeLoaded &= _SongLength.LoadTheme(item + "/SongMenuDetails", "TextSongLength", xmlReader);

            themeLoaded &= _CoverBig.LoadTheme(item + "/SongMenuDetails", "StaticCoverBig", xmlReader);
            themeLoaded &= _TextBG.LoadTheme(item + "/SongMenuDetails", "StaticTextBG", xmlReader);
            themeLoaded &= _DuetIcon.LoadTheme(item + "/SongMenuDetails", "StaticDuetIcon", xmlReader);
            themeLoaded &= _VideoIcon.LoadTheme(item + "/SongMenuDetails", "StaticVideoIcon", xmlReader);
            themeLoaded &= _MedleyCalcIcon.LoadTheme(item + "/SongMenuDetails", "StaticMedleyCalcIcon", xmlReader);
            themeLoaded &= _MedleyTagIcon.LoadTheme(item + "/SongMenuDetails", "StaticMedleyTagIcon", xmlReader);

            if (themeLoaded)
            {
                _Theme.Name = elementName;

                _ReadSubTheme();
                _SubElements.Clear();
                _SubElements.AddRange(new IMenuElement[] { _Artist, _Title, _SongLength, _DuetIcon, _VideoIcon, _MedleyCalcIcon, _MedleyTagIcon });
                LoadSkin();
                Init();
            }

            return themeLoaded;
        }

        private void _ReadSubTheme()
        {
            _Theme.SongMenuDetails.TextArtist = (SThemeText)_Artist.GetTheme();
            _Theme.SongMenuDetails.TextSongLength = (SThemeText)_SongLength.GetTheme();
            _Theme.SongMenuDetails.TextTitle = (SThemeText)_Title.GetTheme();
            _Theme.SongMenuDetails.StaticCoverBig = (SThemeStatic)_CoverBig.GetTheme();
            _Theme.SongMenuDetails.StaticDuetIcon = (SThemeStatic)_DuetIcon.GetTheme();
            _Theme.SongMenuDetails.StaticMedleyCalcIcon = (SThemeStatic)_MedleyCalcIcon.GetTheme();
            _Theme.SongMenuDetails.StaticMedleyTagIcon = (SThemeStatic)_MedleyTagIcon.GetTheme();
            _Theme.SongMenuDetails.StaticTextBG = (SThemeStatic)_TextBG.GetTheme();
            _Theme.SongMenuDetails.StaticVideoIcon = (SThemeStatic)_VideoIcon.GetTheme();
        }

        public override object GetTheme()
        {
            _ReadSubTheme();
            return base.GetTheme();
        }

        public override void Init()
        {
            base.Init();

            _PreviewNr = -1;
            _InitTiles();
        }

        private void _InitTiles()
        {
            MaxRect = SmallView ? _Theme.SongMenuDetails.TileRectSmall : _Theme.SongMenuDetails.TileRect;

            _ListTextWidth = MaxRect.W - _ListTextWidth;

            _TileW = (int)((MaxRect.H - _SpaceH * (_ListLength - 1)) / _ListLength);
            _TileH = _TileW;

            _CoverBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBackground, _PartyModeID);
            _CoverBigBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBigBackground, _PartyModeID);

            //Create cover tiles
            _Tiles = new List<CStatic>();
            _Artists = new List<CText>();
            _Titles = new List<CText>();

            for (int i = 0; i < _ListLength; i++)
            {
                //Create Cover
                var rect = new SRectF(Rect.X, Rect.Y + (_SpaceH/2) + i * (_TileH + _SpaceH), _TileW, _TileH, Rect.Z);
                var tile = new CStatic(_PartyModeID, _CoverBGTexture, _Color, rect);
                _Tiles.Add(tile);

                //Create text
                var artistRect = new SRectF(MaxRect.X + _TileW + _SpaceW, Rect.Y + (_SpaceH / 2) + i * (_TileH + _SpaceH) + (_TileH * 0.55f), _ListTextWidth, _TileH*0.45f, Rect.Z);
                CText artist = new CText(artistRect.X, artistRect.Y, artistRect.Z,
                                       artistRect.H, artistRect.W, EAlignment.Left, EStyle.Normal,
                                       "Outline", _Artist.Color, "");
                artist.MaxRect = new SRectF(artist.MaxRect.X, artist.MaxRect.Y, MaxRect.W + MaxRect.X - artist.Rect.X - 5f, artist.MaxRect.H, artist.MaxRect.Z);
                artist.ResizeAlign = EHAlignment.Center;

                _Artists.Add(artist);

                var titleRect = new SRectF(MaxRect.X + _TileW + _SpaceW, (Rect.Y + (_SpaceH / 2) + i * (_TileH + _SpaceH)), _ListTextWidth, _TileH*0.55f, Rect.Z);
                CText title = new CText(titleRect.X, titleRect.Y, titleRect.Z,
                                       titleRect.H, titleRect.W, EAlignment.Left, EStyle.Bold,
                                       "Outline", _Artist.Color, "");
                title.MaxRect = new SRectF(title.MaxRect.X, title.MaxRect.Y, MaxRect.W + MaxRect.X - title.Rect.X - 5f, title.MaxRect.H, title.MaxRect.Z);
                title.ResizeAlign = EHAlignment.Center;

                _Titles.Add(title);
            }

            _ScrollRect = CBase.Settings.GetRenderRect();
        }

        private void _UpdateTileSelection()
        {
            foreach (CStatic tile in _Tiles)
                tile.Selected = false;

            int tileNr = _SelectionNr - _Offset;
            if (tileNr >= 0 && tileNr < _Tiles.Count)
                _Tiles[tileNr].Selected = true;
        }

        public override void Update(SScreenSongOptions songOptions)
        {
            if (songOptions.Selection.RandomOnly)
                _PreviewNr = _SelectionNr;

            if (_Length < 0 && CBase.Songs.IsInCategory() && CBase.BackgroundMusic.GetLength() > 0)
                _UpdateLength(CBase.Songs.GetVisibleSong(_PreviewNr));
        }

        private void _UpdatePreview()
        {
            //First hide everything so we just have to set what we actually want
            _CoverBig.Texture = _CoverBigBGTexture;
            _Artist.Text = String.Empty;
            _Title.Text = String.Empty;
            _SongLength.Text = String.Empty;
            _DuetIcon.Visible = false;
            _VideoIcon.Visible = false;
            _MedleyCalcIcon.Visible = false;
            _MedleyTagIcon.Visible = false;
            _Length = -1f;

            //Check if nothing is selected (for preview)
            if (_PreviewNr < 0)
                return;

            if (CBase.Songs.IsInCategory())
            {
                CSong song = CBase.Songs.GetVisibleSong(_PreviewNr);
                //Check if we have a valid song (song still visible, index >=0 etc is checked by framework)
                if (song == null)
                {
                    //Display at least the category
                    CCategory category = CBase.Songs.GetCategory(CBase.Songs.GetCurrentCategoryIndex());
                    //Check if we have a valid category
                    if (category == null)
                        return;
                    _CoverBig.Texture = category.CoverTextureBig;
                    _Artist.Text = category.Name;
                    return;
                }
                _CoverBig.Texture = song.CoverTextureBig;
                _Artist.Text = song.Artist;
                _Title.Text = song.Title;
                _DuetIcon.Visible = song.IsDuet;
                _VideoIcon.Visible = song.VideoFileName != "";
                _MedleyCalcIcon.Visible = song.Medley.Source == EDataSource.Calculated;
                _MedleyTagIcon.Visible = song.Medley.Source == EDataSource.Tag;

                _UpdateLength(song);
            }
            else
            {
                CCategory category = CBase.Songs.GetCategory(_PreviewNr);
                //Check if we have a valid category
                if (category == null)
                    return;
                _CoverBig.Texture = category.CoverTextureBig;
                _Artist.Text = category.Name;

                int num = category.GetNumSongsNotSung();
                String songOrSongs = (num == 1) ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
                _Title.Text = CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
            }
        }

        private void _UpdateLength(CSong song)
        {
            if (song == null)
                return;
            float time = CBase.BackgroundMusic.GetLength();
            if (Math.Abs(song.Finish) > 0.001)
                time = song.Finish;

            // The audiobackend is ready to return the length
            if (time > 0)
            {
                time -= song.Start;
                var min = (int)Math.Floor(time / 60f);
                var sec = (int)(time - min * 60f);
                _SongLength.Text = min.ToString("00") + ":" + sec.ToString("00");
                _Length = time;
            }
            else
                _SongLength.Text = "...";
        }

        public override void OnShow()
        {
            _LastKnownElements = -1; //Force refresh of list
            if (!CBase.Songs.IsInCategory())
            {
                if ((CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && CBase.Songs.GetNumCategories() > 0) || CBase.Songs.GetNumCategories() == 1)
                    _EnterCategory(0);
            }
            if (CBase.Songs.IsInCategory())
                SetSelectedSong(_SelectionNr < 0 ? 0 : _SelectionNr);
            else
                SetSelectedCategory(_SelectionNr < 0 ? 0 : _SelectionNr);
            _PreviewNr = _SelectionNr;
            _UpdateListIfRequired();
        }

        public override bool HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions options)
        {
            if (keyEvent.KeyPressed)
                return false;

            bool moveAllowed = !options.Selection.RandomOnly || (options.Selection.CategoryChangeAllowed && !CBase.Songs.IsInCategory());
            bool catChangePossible = CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && options.Selection.CategoryChangeAllowed;

            //If nothing selected set a reasonable default value
            if (keyEvent.IsArrowKey() && moveAllowed && _SelectionNr < 0)
                _SelectionNr = (_PreviewNr < 0) ? _Offset : _PreviewNr;

            switch (keyEvent.Key)
            {
                case Keys.Enter:
                    if (CBase.Songs.IsInCategory())
                    {
                        if (_SelectionNr >= 0 && _PreviewNr != _SelectionNr)
                        {
                            _PreviewNr = _SelectionNr;
                            keyEvent.Handled = true;
                        }
                    }
                    else
                    {
                        _EnterCategory(_PreviewNr);
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.Escape:
                case Keys.Back:
                    if (CBase.Songs.IsInCategory() && catChangePossible)
                    {
                        _LeaveCategory();
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.PageUp:
                    if (catChangePossible)
                    {
                        _PrevCategory();
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.PageDown:
                    if (catChangePossible)
                    {
                        _NextCategory();
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.Left:
                    //Check for >0 so we do not allow selection of nothing (-1)
                    if (_SelectionNr > 0 && moveAllowed)
                    {
                        _SelectionNr--;
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.Right:
                    if (moveAllowed)
                    {
                        _SelectionNr++;
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.Up:
                    if (keyEvent.ModShift)
                    {
                        if (catChangePossible)
                        {
                            _PrevCategory();
                            keyEvent.Handled = true;
                        }
                    }
                    else if (_SelectionNr >= 1 && moveAllowed)
                    {
                        _SelectionNr -= 1;
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.Down:
                    if (keyEvent.ModShift)
                    {
                        if (catChangePossible)
                        {
                            _NextCategory();
                            keyEvent.Handled = true;
                        }
                    }
                    else if (moveAllowed)
                    {
                        _SelectionNr += 1;
                        keyEvent.Handled = true;
                    }
                    break;
            }
            if (!CBase.Songs.IsInCategory())
                _PreviewNr = _SelectionNr;
            return keyEvent.Handled;
        }

        public override bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions)
        {
            if (!songOptions.Selection.RandomOnly || (!CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed))
            {
                if (mouseEvent.Wheel != 0 && CHelper.IsInBounds(_ScrollRect, mouseEvent))
                    _UpdateList(_Offset +  mouseEvent.Wheel);

                int lastSelection = _SelectionNr;
                int i = 0;
                bool somethingSelected = false;

                foreach (CStatic tile in _Tiles)
                {
                    //create a rect including the cover and text of the song. 
                    //(this way mouse over text should make a selection as well)
                    SRectF songRect = new SRectF(tile.Rect.X, tile.Rect.Y, Rect.W, tile.Rect.H, tile.Rect.Z);
                    if (tile.Texture != _CoverBGTexture && CHelper.IsInBounds(songRect, mouseEvent) && tile.Color.A != 0)
                    {
                        somethingSelected = true;
                        _SelectionNr = i + _Offset;
                        if (!CBase.Songs.IsInCategory())
                            _PreviewNr = i + _Offset;
                        break;
                    }
                    i++;
                }
                //Reset selection only if we moved out of the rect to avoid loosing it when selecting random songs
                if (_MouseWasInRect && !somethingSelected)
                    _SelectionNr = -1;
                if (mouseEvent.Sender == ESender.WiiMote && _SelectionNr != lastSelection && _SelectionNr != -1)
                    CBase.Controller.SetRumble(0.050f);
            }
            _MouseWasInRect = CHelper.IsInBounds(Rect, mouseEvent);

            if (mouseEvent.RB)
            {
                if (CBase.Songs.IsInCategory() && CBase.Songs.GetNumCategories() > 0 && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON &&
                    songOptions.Selection.CategoryChangeAllowed)
                {
                    _LeaveCategory();
                    return true;
                }
                if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && !songOptions.Selection.PartyMode)
                {
                    CBase.Graphics.FadeTo(EScreen.Main);
                    return true;
                }
            }
            else if (mouseEvent.LB)
            {
                if (_SelectionNr >= 0 && _MouseWasInRect)
                {
                    if (CBase.Songs.IsInCategory())
                    {
                        if (_PreviewNr == _SelectionNr)
                            return false;
                        _PreviewNr = _SelectionNr;
                    }
                    else
                        EnterSelectedCategory();
                    return true;
                }
            }
            return false;
        }

        public override void Draw()
        {
            foreach (CStatic tile in _Tiles)
            {
                if (tile.Selected)
                    tile.Color.A = 1f;
                else
                    tile.Color.A = 0.6f;
                EAspect aspect = (tile.Texture != _CoverBGTexture) ? EAspect.Crop : EAspect.Stretch;
                tile.Draw(aspect);
            }

            //highlight the text of the selected song
            int i = 0;
            foreach (CText text in _Artists)
            {
                if (i < _Tiles.Count && _Tiles[i].Selected)
                    text.Color.A = 1f;
                else if (i < _Tiles.Count)
                    text.Color.A = 0.6f;
                else
                    text.Text = "";
                text.Draw();
                i++;
            }
            i = 0;
            foreach (CText text in _Titles)
            {
                if (i < _Tiles.Count && _Tiles[i].Selected)
                    text.Color.A = 1f;
                else if (i < _Tiles.Count)
                    text.Color.A = 0.6f;
                else
                    text.Text = "";
                text.Draw();
                i++;
            }
            _TextBG.Draw();

            CTextureRef vidtex = CBase.BackgroundMusic.IsPlayingPreview() ? CBase.BackgroundMusic.GetVideoTexture() : null;

            if (vidtex != null)
            {
                if (vidtex.Color.A < 1)
                    _CoverBig.Draw(EAspect.Crop);
                SRectF rect = CHelper.FitInBounds(_CoverBig.Rect, vidtex.OrigAspect, EAspect.Crop);
                CBase.Drawing.DrawTexture(vidtex, rect, vidtex.Color, _CoverBig.Rect);
                CBase.Drawing.DrawTextureReflection(vidtex, rect, vidtex.Color, _CoverBig.Rect, _CoverBig.ReflectionSpace, _CoverBig.ReflectionHeight);
            }
            else
                _CoverBig.Draw(EAspect.Crop);

            foreach (IMenuElement element in _SubElements)
                element.Draw();
        }

        public override CStatic GetSelectedSongCover()
        {
            return _Tiles.FirstOrDefault(tile => tile.Selected);
        }

        public override bool IsMouseOverSelectedSong(SMouseEvent mEvent)
        {
            for (int i = 0; i < _Tiles.Count; i++)
            {
                if (!_Tiles[i].Selected)
                    continue;
                return CHelper.IsInBounds(_Tiles[i].Rect, mEvent) || CHelper.IsInBounds(_Artists[i].Rect, mEvent) || CHelper.IsInBounds(_Titles[i].Rect, mEvent);
            }
            return false;
        }

        protected override void _EnterCategory(int categoryNr)
        {
            base._EnterCategory(categoryNr);

            SetSelectedSong(0);
            _UpdateListIfRequired();
        }

        protected override void _LeaveCategory()
        {
            base._LeaveCategory();

            SetSelectedCategory(0);
            _UpdateListIfRequired();
        }

        private void _NextCategory()
        {
            if (CBase.Songs.IsInCategory())
            {
                CBase.Songs.NextCategory();
                _EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void _PrevCategory()
        {
            if (CBase.Songs.IsInCategory())
            {
                CBase.Songs.PrevCategory();
                _EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void _UpdateListIfRequired()
        {
            int curElements = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
            if ((_LastKnownElements == curElements) && (_LastKnownCategory == CBase.Songs.GetCurrentCategoryIndex()))
                return;

            _LastKnownCategory = CBase.Songs.GetCurrentCategoryIndex();
            _LastKnownElements = curElements;
            CBase.Songs.UpdateRandomSongList();
            _UpdateList(true);
        }

        private void _UpdateList(bool force = false)
        {
            int offset;
            if (_SelectionNr < _Offset && _SelectionNr >= 0)
                offset = _SelectionNr;
            else if (_SelectionNr >= _Offset + _ListLength)
                offset = _SelectionNr - _ListLength + 1;
            else
                offset = _Offset;
            _UpdateList(offset, force);
        }

        private void _UpdateList(int offset, bool force = false)
        {
            bool isInCategory = CBase.Songs.IsInCategory();
            int itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
            int totalSongNumber = CBase.Songs.GetNumSongs();

            offset = offset.Clamp(0, itemCount - _ListLength, true);

            if (offset == _Offset && !force)
                return;

            for (int i = 0; i < _Tiles.Count; i++)
            {
                if (i + offset < itemCount)
                {
                    _Tiles[i].Color = new SColorF(1f, 1f, 1f, 1f);
                    if (isInCategory)
                    {
                        CSong currentSong = CBase.Songs.GetVisibleSong(i + offset);
                        _Tiles[i].Texture = currentSong.CoverTextureSmall;
                        _Artists[i].Text = currentSong.Artist;
                        _Titles[i].Text = currentSong.Title;
                    }
                    else
                    {
                        CCategory currentCat = CBase.Songs.GetCategory(i + offset);
                        _Tiles[i].Texture = currentCat.CoverTextureSmall;
                        int num = currentCat.GetNumSongsNotSung();
                        String songOrSongs = (num == 1) ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
                        _Titles[i].Text = currentCat.Name;
                        _Artists[i].Text = CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
                    }
                }
                else
                {
                    _Tiles[i].Color.A = 0;
                    _Artists[i].Text = "";
                    _Titles[i].Text = "";
                }
            }
            _Offset = offset;
        }

        public override void LoadSkin()
        {
            foreach (IThemeable themeable in _SubElements.OfType<IThemeable>())
                themeable.LoadSkin();
            // Those are drawn seperately so they are not in the above list
            _CoverBig.LoadSkin();
            _TextBG.LoadSkin();

            base.LoadSkin();
        }
    }
}