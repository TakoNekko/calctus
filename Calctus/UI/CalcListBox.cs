﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Shapoco.Calctus.Model;

namespace Shapoco.Calctus.UI {
    class CalcListBox : ContainerControl {
        public event EventHandler RadixModeChanged;

        private List<CalcListItem> _items = new List<CalcListItem>();
        private int _selectedIndex = -1;
        private CalcListItem _selectedItem = null;
        private Panel _innerPanel = new Panel();
        private VScrollBar _scrollBar = new VScrollBar();
        private RadixMode _radixMode = RadixMode.Auto;

        private ContextMenuStrip _ctxMenu = new ContextMenuStrip();
        private ToolStripMenuItem _cmenuTextCut = new ToolStripMenuItem("Cut Text");
        private ToolStripMenuItem _cmenuTextCopy = new ToolStripMenuItem("Copy Text");
        private ToolStripMenuItem _cmenuTextPaste = new ToolStripMenuItem("Paste Text");
        private ToolStripMenuItem _cmenuTextDelete = new ToolStripMenuItem("Delete Text");
        private ToolStripSeparator _cmenuSep0 = new ToolStripSeparator();
        private ToolStripMenuItem _cmenuCopyAll = new ToolStripMenuItem("Copy All");
        private ToolStripSeparator _cmenuSep1 = new ToolStripSeparator();
        private ToolStripMenuItem _cmenuMoveUp = new ToolStripMenuItem("Move Up");
        private ToolStripMenuItem _cmenuMoveDown = new ToolStripMenuItem("Move Down");
        private ToolStripSeparator _cmenuSep2 = new ToolStripSeparator();
        private ToolStripMenuItem _cmenuItemInsert = new ToolStripMenuItem("Insert Item");
        private ToolStripMenuItem _cmenuItemDelete = new ToolStripMenuItem("Delete Item");
        private ToolStripSeparator _cmenuTextSep2 = new ToolStripSeparator();
        private ToolStripMenuItem _cmenuClear = new ToolStripMenuItem("Clear");

        public CalcListBox() {
            if (this.DesignMode) return;
            _scrollBar.TabStop = false;
            _scrollBar.ValueChanged += (sender, e) => { _innerPanel.Top = -((VScrollBar)_scrollBar).Value; };
            this.Controls.Add(_innerPanel);
            this.Controls.Add(_scrollBar);
            this.insert(0, new CalcListItem(this));
            this.SelectedIndex = 0;

            this.MouseUp += (sender, e) => {
                if (e.Button == MouseButtons.Right) {
                    openContextMenu(this.PointToScreen(e.Location));
                }
            };
            //this.Click += (sender, e) => { this.SelectedIndex = -1; };

            _cmenuTextCut.ShortcutKeyDisplayString = "Ctrl+X";
            _cmenuTextCopy.ShortcutKeyDisplayString = "Ctrl+C";
            _cmenuTextPaste.ShortcutKeyDisplayString = "Ctrl+V";
            _cmenuCopyAll.ShortcutKeyDisplayString = "Ctrl+Shift+C";
            _cmenuMoveUp.ShortcutKeyDisplayString = "Ctrl+Shift+Up";
            _cmenuMoveDown.ShortcutKeyDisplayString = "Ctrl+Shift+Down";
            _cmenuItemInsert.ShortcutKeyDisplayString = "Shift+Enter";
            _cmenuItemDelete.ShortcutKeyDisplayString = "Shift+Del";
            _cmenuClear.ShortcutKeyDisplayString = "Ctrl+Shift+Del";

            _cmenuTextCut.Click += (sender, e) => { this.SelectedItem?.OnCutText(); };
            _cmenuTextCopy.Click += (sender, e) => { this.SelectedItem?.OnCopyText(); };
            _cmenuTextPaste.Click += (sender, e) => { this.SelectedItem?.OnPasteText(); };
            _cmenuTextDelete.Click += (sender, e) => { this.SelectedItem?.OnDeleteText(); };
            _cmenuCopyAll.Click += (sender, e) => { this.CopyAll(); };
            _cmenuMoveUp.Click += (sender, e) => { this.ItemMoveUp(); };
            _cmenuMoveDown.Click += (sender, e) => { this.ItemMoveDown(); };
            _cmenuItemInsert.Click += (sender, e) => { this.ItemInsert(); };
            _cmenuItemDelete.Click += (sender, e) => { this.ItemDelete(); };
            _cmenuClear.Click += (sender, e) => { this.Clear(); };

            _ctxMenu.Items.AddRange(new ToolStripItem[] {
                _cmenuTextCut,
                _cmenuTextCopy,
                _cmenuTextPaste,
                _cmenuTextDelete,
                _cmenuSep0,
                _cmenuCopyAll,
                _cmenuSep1,
                _cmenuMoveUp,
                _cmenuMoveDown,
                _cmenuSep2,
                _cmenuItemInsert,
                _cmenuItemDelete,
                _cmenuTextSep2,
                _cmenuClear
            });
        }

        public Panel InnerPanel => _innerPanel;

        public int SelectedIndex {
            get => _selectedIndex;
            set {
                if (value == _selectedIndex) return;
                performSelectedIndexChanged(value);
            }
        }

        public CalcListItem SelectedItem {
            get => _selectedItem;
            set {
                if (value == null) this.SelectedIndex = -1;
                int i = _items.IndexOf(value);
                if (i < 0) throw new IndexOutOfRangeException();
                this.SelectedIndex = i;
            }
        }

        public RadixMode RadixMode {
            get => _radixMode;
            set {
                if (value == _radixMode) return;
                _radixMode = value;
                if (this.SelectedIndex >= 0) {
                    _items[this.SelectedIndex].RadixMode = value;
                    recalc(this.SelectedIndex);
                }
                RadixModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CopyAll() {
            var sb = new StringBuilder();
            foreach(var item in _items) {
                sb.Append(item.Expression).Append(" = ").AppendLine(item.Answer);
            }
            try {
                Clipboard.Clear();
                Clipboard.SetText(sb.ToString());
            }
            catch {
                System.Media.SystemSounds.Beep.Play();
            }
        }

        public void ItemMoveUp() {
            var selIndex = this.SelectedIndex;
            var selItem = this.SelectedItem;
            if (selIndex < 1 || selItem == null) return;

            _items.RemoveAt(selIndex);
            _items.Insert(selIndex - 1, selItem);
            performSelectedIndexChanged(selIndex - 1);
            recalc(selIndex, selIndex - 1);
            relayout();
        }

        public void ItemMoveDown() {
            var selIndex = this.SelectedIndex;
            var selItem = this.SelectedItem;
            if (selIndex >= _items.Count - 1 || selItem == null) return;

            _items.RemoveAt(selIndex);
            _items.Insert(selIndex + 1, selItem);
            performSelectedIndexChanged(selIndex + 1);
            recalc(selIndex, selIndex + 1);
            relayout();
        }

        public void ItemInsert() {
            var insIndex = this.SelectedIndex;
            if (insIndex < 0) {
                insIndex = _items.Count;
            }
            insert(insIndex, new CalcListItem(this));
            performSelectedIndexChanged(insIndex);
            recalc(insIndex);
            relayout();
        }

        public void ItemDelete() {
            var selIndex = this.SelectedIndex;
            var selItem = this.SelectedItem;
            if (selItem == null) return;
            delete(this.SelectedItem);
            if (_items.Count == 0) {
                insert(0, new CalcListItem(this));
            }
            if (selIndex < _items.Count) {
                performSelectedIndexChanged(selIndex);
                recalc(selIndex);
            }
            else {
                performSelectedIndexChanged(selIndex - 1);
                recalc(selIndex - 1);
            }
            relayout();
        }

        public void Clear() {
            var ans = MessageBox.Show("Are you sure you want to delete all?", "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            if (ans == DialogResult.OK) {
                while (_items.Count > 0) {
                    delete(_items[_items.Count - 1]);
                }
                insert(0, new CalcListItem(this));
                performSelectedIndexChanged(0);
                recalc();
                relayout();
            }
        }

        public void Recalc() {
            recalc();
        }

        public void Refocus() {
            performSelectedIndexChanged(this.SelectedIndex);
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            relayout();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            relayout();
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);
            if (_scrollBar.Visible) {
                int min = _scrollBar.Minimum;
                int max = _scrollBar.Maximum - _scrollBar.LargeChange;
                _scrollBar.Value = Math.Max(min, Math.Min(max, _scrollBar.Value - e.Delta));
            }
        }

        private void performSelectedIndexChanged(int newIndex) {
            if (_selectedItem != null) {
                _selectedItem.OnDeselected();
            }
            _selectedIndex = newIndex;
            if (newIndex >= 0) {
                _selectedItem = _items[_selectedIndex];
                _selectedItem.OnSelected();
                showItem(_selectedIndex);
                this.RadixMode = _selectedItem.RadixMode;
            }
            else {
                _selectedItem = null;
            }
        }

        private void insert(int index, CalcListItem item) {
            _items.Insert(index, item);
            _innerPanel.Controls.Add(item);
            item.ExpressionChanged += Item_ExpressionChanged;
            item.ItemKeyDown += Item_PreviewKeyDown;
            item.ItemKeyUp += Item_KeyUp;
            item.ItemGotFocus += Item_GotFocus;
            item.ItemMouseUp += Item_MouseUp;
        }

        private void delete(CalcListItem item) {
            item.ExpressionChanged -= Item_ExpressionChanged;
            item.ItemKeyDown -= Item_PreviewKeyDown;
            item.ItemKeyUp -= Item_KeyUp;
            item.ItemGotFocus -= Item_GotFocus;
            item.ItemMouseUp -= Item_MouseUp;
            _innerPanel.Controls.Remove(item);
            _items.Remove(item);
        }

        private void Item_GotFocus(object sender, EventArgs e) {
            var item = (CalcListItem)sender;
            int index = _items.IndexOf(item);
            if (index >= 0) {
                this.SelectedIndex = index;
            }
            else {
                this.SelectedIndex = -1;
            }
        }

        private void Item_ExpressionChanged(object sender, EventArgs e) {
            relayout();
            recalc(this.SelectedIndex);
        }

        private void Item_PreviewKeyDown(object sender, KeyEventArgs e) {
            var item = (CalcListItem)sender;
            int index = _items.IndexOf(item);
            if (index < 0) return;
            if (e.KeyCode == Keys.Return) { // Return
                if (e.Modifiers == Keys.None) {
                    e.Handled = true;
                    if (this.SelectedIndex < _items.Count - 1) {
                        this.SelectedIndex++;
                    }
                    else {
                        var newItem = new CalcListItem(this, item);
                        this.insert(index + 1, newItem);
                        relayout();
                        this.SelectedIndex = index + 1;
                    }
                }
                else if (e.Modifiers == Keys.Shift) {
                    e.Handled = true;
                    this.insert(index, new CalcListItem(this));
                    relayout();
                    performSelectedIndexChanged(this.SelectedIndex);
                }
            }
            else if (e.KeyCode == Keys.Up && e.Modifiers == Keys.None) {
                if (this.SelectedIndex > 0) {
                    e.Handled = true;
                    this.SelectedIndex--;
                }
            }
            else if (e.KeyCode == Keys.Down && e.Modifiers == Keys.None) {
                if (this.SelectedIndex < _items.Count - 1) {
                    e.Handled = true;
                    this.SelectedIndex++;
                }
            }
            else if (e.KeyCode == Keys.Up && e.Modifiers == (Keys.Control | Keys.Shift)) {
                if (this.SelectedIndex > 0) {
                    e.Handled = true;
                    ItemMoveUp();
                }
            }
            else if (e.KeyCode == Keys.Down && e.Modifiers == (Keys.Control | Keys.Shift)) {
                if (this.SelectedIndex < _items.Count - 1) {
                    e.Handled = true;
                    ItemMoveDown();
                }
            }
            else if (e.KeyCode == Keys.PageUp && e.Modifiers == Keys.None) {
                if (this.SelectedIndex > 0) {
                    e.Handled = true;
                    pageUp();
                }
            }
            else if (e.KeyCode == Keys.PageDown && e.Modifiers == Keys.None) {
                if (this.SelectedIndex < _items.Count - 1) {
                    e.Handled = true;
                    pageDown();
                }
            }
            else if (e.KeyCode == Keys.C && e.Modifiers == (Keys.Control | Keys.Shift)) {
                e.Handled = true;
                CopyAll();
            }
            else if (e.KeyCode == Keys.Delete && e.Modifiers == Keys.Shift) {
                e.Handled = true;
                ItemDelete();
            }
            else if (e.KeyCode == Keys.Delete && e.Modifiers == (Keys.Control | Keys.Shift)) {
                e.Handled = true;
                Clear();
            }
        }

        private void Item_KeyUp(object sender, KeyEventArgs e) {
            var item = (CalcListItem)sender;
            if (e.KeyCode == Keys.Apps && e.Modifiers == Keys.None) {
                openContextMenu(item.PointToScreen(new Point(0, item.Height)));
            }
        }

        private void Item_MouseUp(object sender, MouseEventArgs e) {
            var item = (CalcListItem)sender;
            if (e.Button == MouseButtons.Right) {
                openContextMenu(item.PointToScreen(e.Location));
            }
        }

        private void pageUp() {
            var selItem = this.SelectedItem;
            if (selItem != null) {
                int y = selItem.Bottom - this.ClientSize.Height;
                this.SelectedIndex = indexFromClientY(y);
            }
        }

        private void pageDown() {
            var selItem = this.SelectedItem;
            if (selItem != null) {
                int y = selItem.Top + this.ClientSize.Height;
                this.SelectedIndex = indexFromClientY(y);
            }
        }

        private int indexFromClientY(int y) {
            if (_items.Count == 0) return -1;
            if (y < 0) return 0;
            for(int i = 0; i < _items.Count; i++) {
                if (y < _items[i].Bottom) {
                    return i;
                }
            }
            return _items.Count - 1;
        }

        private void openContextMenu(Point screenPos) {
            var selIndex = this.SelectedIndex;
            var selItem = this.SelectedItem;
            _cmenuTextCut.Enabled = selItem != null && selItem.IsTextCuttable;
            _cmenuTextCopy.Enabled = selItem != null && selItem.IsTextCopiable;
            _cmenuTextPaste.Enabled = selItem != null && selItem.IsTextPastable;
            _cmenuTextDelete.Enabled = selItem != null && selItem.IsTextCuttable;
            _cmenuMoveUp.Enabled = selItem != null && selIndex > 0;
            _cmenuMoveDown.Enabled = selItem != null && selIndex < _items.Count - 1;
            _cmenuItemDelete.Enabled = selItem != null;
            _ctxMenu.Show(screenPos);
        }

        private void showItem(int index) {
            if (index < 0 || index >= _items.Count) return;
            var client = this.ClientSize;
            var item = _items[index];

            if (_innerPanel.Top + item.Top < 0) {
                _scrollBar.Value = item.Top;
            }
            else if (_innerPanel.Top + item.Bottom > client.Height) {
                _scrollBar.Value = item.Bottom - client.Height;
            }
        }

        private void relayout() {
            if (_items.Count == 0) return;
            var client = this.ClientSize;
            int scrollBarWidth = _scrollBar.PreferredSize.Width;
            int itemWidth = client.Width - scrollBarWidth;
            int y = 0;
            int tabIndex = 0;
            _scrollBar.SetBounds(client.Width - scrollBarWidth, 0, scrollBarWidth, client.Height);
            foreach (var item in _items) {
                int itemHeight = item.GetPreferredSize(new Size(itemWidth, int.MaxValue)).Height;
                item.SetBounds(0, y, itemWidth, itemHeight);
                item.TabIndex = tabIndex++;
                y += itemHeight;
            }
            _innerPanel.Size = new Size(itemWidth, y);
            if (_innerPanel.Height < client.Height) {
                _scrollBar.Visible = false;
                _scrollBar.Minimum = 0;
                _scrollBar.Maximum = 0;
            }
            else {
                _scrollBar.Minimum = 0;
                _scrollBar.Maximum = Math.Max(0, _innerPanel.Height);
                _scrollBar.LargeChange = client.Height;
                _scrollBar.SmallChange = 20;
                _scrollBar.Visible = true;
            }
        }

        private void recalc() {
            recalc(_items);
        }

        private void recalc(params int[] indices) {
            var items = new CalcListItem[indices.Length];
            for (var i = 0; i < indices.Length; ++i) {
                items[i] = _items[indices[i]];
            }
            recalc(items);
        }

        private void recalc(IEnumerable<CalcListItem> items) {
            EvalContext ctx = new EvalContext();

            // 設定を評価コンテキストに反映する
            var s = Settings.Instance;
            ctx.Settings.DecimalLengthToDisplay = s.NumberFormat_Decimal_MaxLen;
            ctx.Settings.ENotationEnabled = s.NumberFormat_Exp_Enabled;
            ctx.Settings.ENotationExpPositiveMin = s.NumberFormat_Exp_PositiveMin;
            ctx.Settings.ENotationExpNegativeMax = s.NumberFormat_Exp_NegativeMax;
            ctx.Settings.ENotationAlignment = s.NumberFormat_Exp_Alignment;

            foreach (var item in items) {
                try {
                    var expr = Parser.Parser.Parse(item.Expression);
                    var val = expr.Eval(ctx);
            
                    switch (item.RadixMode) {
                        case RadixMode.Dec: val = val.FormatInt(); break;
                        case RadixMode.Hex: val = val.FormatHex(); break;
                        case RadixMode.Bin: val = val.FormatBin(); break;
                        case RadixMode.Oct: val = val.FormatOct(); break;
                    }
            
                    item.Answer = val.ToString(ctx);
                    item.Hint = "";
                    ctx.Ref("Ans", true).Value = val;
                }
                catch (Exception ex) {
                    item.Answer = "";
                    item.Hint = "? " + ex.Message;
                    ctx.Undef("Ans", true);
                }
            }
        }
    }

}
