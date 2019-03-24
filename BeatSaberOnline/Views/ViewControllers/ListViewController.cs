using CustomUI.BeatSaber;
using CustomUI.Utilities;
using HMUI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberOnline.Views.ViewControllers
{
    class ListViewController : CustomListViewController
    {
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
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

                _customListTableView.didSelectCellWithIdxEvent += _customListTableView_didSelectRowEvent;

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

        private void _customListTableView_didSelectRowEvent(TableView arg1, int arg2)
        {
            DidSelectRowEvent?.Invoke(arg1, arg2);
        }

        override public TableCell CellForIdx(int idx)
        {
            LevelListTableCell _tableCell = GetTableCell(idx);
            _tableCell.SetText(Data[idx].text);
            _tableCell.SetSubText(Data[idx].subtext);
            _tableCell.SetIcon(Data[idx].icon == null ? UIUtilities.BlankSprite : Data[idx].icon);
            return _tableCell;
        }
    }
}
