﻿using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Toolkit.Controls
{
    public partial class Gallery : UserControl
    {
        public Gallery()
        {
            InitializeComponent();
        }

        private void ListViewItems_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenToolStripMenuItem_Click(sender, e);
        }

        private async void Gallery_Load(object sender, EventArgs e)
        {
            listViewItems.DataBindings.Add("BackColor", this, "BackColor");
            listViewItems.DataBindings.Add("ForeColor", this, "ForeColor");

            Width = (int)(FindForm().Width * 0.3);
            FindForm().Resize += (s, v) =>
            {
                Width = (int)((s as Form).Width * 0.3);
            };
            fileSystemWatcher.Path = Path.Combine(Properties.Settings.Default.DefaultPath, Program.AlbumName);
            await PopulateAsync();
        }

        private async Task PopulateAsync()
        {
            var path = new DirectoryInfo(fileSystemWatcher.Path);
            var files = path.GetFiles().Where(m => Program.Formats.Contains(m.Extension)).ToList();

            var images = await Task.Run(() =>
                         (from item in files
                          let index = files.IndexOf(item)
                          let image = Image.FromFile(item.FullName)
                          let thumbnail = image.GetThumbnailImage(imageListThumbs.ImageSize.Width, imageListThumbs.ImageSize.Height, null, IntPtr.Zero)
                          select new { item.FullName, Index = index, Thumbnail = thumbnail, Size = (item.Length / 1024) }).ToList());

            var data = from item in images
                       select new ListViewItem(new[] { Path.GetFileNameWithoutExtension(item.FullName), item.FullName, $"{item.Size.ToString()} Kb" }, item.Index);

            this.Invoke(new Action(() =>
            {
                imageListThumbs.Images.Clear();
                listViewItems.Items.Clear();
                imageListThumbs.Images.AddRange(images.Select(m => m.Thumbnail).ToArray());
                listViewItems.Items.AddRange(data.ToArray());

                listViewItems.Visible = listViewItems.Items.Count > 0;
                labelInformation.Visible = !listViewItems.Visible;

                images = null;
                data = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }));
        }

        private async void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (Program.Formats.Contains(Path.GetExtension(e.FullPath)))
                await PopulateAsync();
        }

        private async void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (Program.Formats.Contains(Path.GetExtension(e.FullPath)) || Program.Formats.Contains(Path.GetExtension(e.OldFullPath)))
                await PopulateAsync();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewItems.SelectedItems != null && listViewItems.SelectedItems.OfType<ListViewItem>().Any())
                foreach (var item in listViewItems.SelectedItems)
                    Process.Start((item as ListViewItem).SubItems[1].Text);
        }

        private void ContextMenuStripActions_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var hasItemSelected = (listViewItems.SelectedItems != null && listViewItems.SelectedItems.OfType<ListViewItem>().Any());
            openToolStripMenuItem.Enabled = hasItemSelected;
            deleteToolStripMenuItem.Enabled = hasItemSelected;
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewItems.SelectedItems != null && listViewItems.SelectedItems.OfType<ListViewItem>().Any())
                foreach (var item in listViewItems.SelectedItems)
                {
                    try
                    {
                        File.Delete((item as ListViewItem).SubItems[1].Text);
                    }
                    catch (Exception)
                    {
                    }
                }
        }

        public void Close()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
            fileSystemWatcher = null;
            Parent.Controls.Remove(this);
            Dispose();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void ListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewItems.View = View.List;
        }

        private void SmallIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewItems.View = View.SmallIcon;
        }

        private void LargeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewItems.View = View.LargeIcon;
        }

        private void DetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewItems.View = View.Details;
        }

        private void TilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewItems.View = View.Tile;
        }
    }
}