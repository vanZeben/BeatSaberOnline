using CustomUI.BeatSaber;
using CustomUI.Utilities;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace BeatSaberOnline.Views.ViewControllers
{
    class ListViewController : CustomViewController, TableView.IDataSource
    {
        public Button _pageUpButton;
        public Button _pageDownButton;
        public TableView _customListTableView;
        public List<CustomCellInfo> Data = new List<CustomCellInfo>();
        public Action<TableView, int> DidSelectRowEvent;

        private LevelListTableCell _songListTableCellInstance;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation)
            {
                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                RectTransform container = new GameObject("CustomListContainer", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.sizeDelta = new Vector2(60f, 0f);

                _customListTableView = new GameObject("CustomListTableView").AddComponent<TableView>();
                _customListTableView.gameObject.AddComponent<RectMask2D>();
                _customListTableView.transform.SetParent(container, false);
                (_customListTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (_customListTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_customListTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 50f);
                (_customListTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -5f);

                _customListTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _customListTableView.SetPrivateField("_isInitialized", false);
                _customListTableView.dataSource = this;

                _customListTableView.didSelectRowEvent += _customListTableView_didSelectRowEvent;

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), container, false);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 20f);//-14
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _customListTableView.PageScrollUp();
                });

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), container, false);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -30f);//8
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _customListTableView.PageScrollDown();
                });
            }
            base.DidActivate(firstActivation, type);
        }
        protected override void DidDeactivate(DeactivationType type)
        {
            base.DidDeactivate(type);
        }

        private void _customListTableView_didSelectRowEvent(TableView arg1, int arg2)
        {
            DidSelectRowEvent?.Invoke(arg1, arg2);
        }

        public virtual float RowHeight()
        {
            return 10f;
        }

        public virtual int NumberOfRows()
        {
            return Data.Count;
        }

        public virtual TableCell CellForRow(int row)
        {
            LevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);
            _tableCell.songName = Data[row].text;
            _tableCell.author = Data[row].subtext;
            _tableCell.coverImage = Data[row].icon == null ? UIUtilities.BlankSprite : Data[row].icon;
            _tableCell.reuseIdentifier = "CustomListCell";
            return _tableCell;
        }
    }
}
