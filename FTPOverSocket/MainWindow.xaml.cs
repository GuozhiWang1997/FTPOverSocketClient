﻿using FTPOverSocket.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FTPOverSocket
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private SocketService socket = SocketService.getInstance();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawFile(int row, int column, int type, string filename)
        {
            Image image = new Image();
            image.Height = 50;
            image.VerticalAlignment = VerticalAlignment.Top;
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            switch(type)
            {
                case 1: bitmap.UriSource = new Uri("pack://application:,,,/Images/file.png"); break;
                case 2: bitmap.UriSource = new Uri("pack://application:,,,/Images/file_cloud.png"); break;
                case 3: bitmap.UriSource = new Uri("pack://application:,,,/Images/file_download.png"); break;
                case 4: bitmap.UriSource = new Uri("pack://application:,,,/Images/file_upload.png"); break;
            }
            bitmap.EndInit();
            image.Source = bitmap;

            ContextMenu menu = new ContextMenu();
            MenuItem item1 = new MenuItem();
            item1.Header = "Download";
            item1.DataContext = filename + "?Download";
            item1.AddHandler(MenuItem.MouseUpEvent, new MouseButtonEventHandler(Download_Click), true);
            MenuItem item2 = new MenuItem();
            item2.Header = "Open";
            item2.DataContext = filename + "?Open";
            item2.AddHandler(MenuItem.MouseUpEvent, new MouseButtonEventHandler(Open_Click), true);
            MenuItem item3 = new MenuItem();
            item3.Header = "Remove";
            item3.DataContext = filename + "?Remove";
            item3.AddHandler(MenuItem.MouseUpEvent, new MouseButtonEventHandler(Remove_Click), true);
            MenuItem item4 = new MenuItem();
            item4.Header = "Details";
            item4.DataContext = filename + "?Details";
            item4.AddHandler(MenuItem.MouseUpEvent, new MouseButtonEventHandler(Details_Click), true);
            menu.Items.Add(item1);
            if (type == 1)
            {
                item1.Header = "Update";
                menu.Items.Add(item2);
            }
            menu.Items.Add(item3);
            menu.Items.Add(item4);
            image.ContextMenu = menu;

            Label label = new Label();
            label.Content = filename;
            label.Height = 20;
            label.VerticalAlignment = VerticalAlignment.Bottom;
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            label.VerticalContentAlignment = VerticalAlignment.Center;
            label.FontSize = 9;

            gridFiles.Children.Add(image);
            gridFiles.Children.Add(label);
            Grid.SetRow(image, row);
            Grid.SetRow(label, row);
            Grid.SetColumn(image, column);
            Grid.SetColumn(label, column);
        }

        public void Login_Success()
        {
            string address = textBoxServerAddress.Text;
            int port = int.Parse(textBoxPort.Text);

            this.Refresh_Grid();

            textBoxServerAddress.IsEnabled = false;
            textBoxPort.IsEnabled = false;
            buttonConnect.Content = "Disconnect";
            buttonUpload.IsEnabled = true;

            ContextMenu menu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "Refresh";
            item.DataContext = "Refresh";
            item.AddHandler(MenuItem.MouseUpEvent, new MouseButtonEventHandler(Refresh_Click), true);
            menu.Items.Add(item);
            gridBackground.ContextMenu = menu;
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!buttonUpload.IsEnabled)
            {
                LoginWindow loginWindow = new LoginWindow(textBoxServerAddress.Text, textBoxPort.Text);
                loginWindow.ShowDialog();
                if (loginWindow.DialogResult == true)
                {
                    this.Login_Success();
                }
            }
            else
            {
                this.socket.Close();
                gridFiles.Children.RemoveRange(0, int.MaxValue);
                textBoxServerAddress.IsEnabled = true;
                textBoxPort.IsEnabled = true;
                buttonConnect.Content = "Connect";
                buttonUpload.IsEnabled = false;
                gridBackground.ContextMenu = null;
            }
        }

        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result = openFileDlg.ShowDialog();
            if (result == true)
            {
                string filepath = openFileDlg.FileName;
                string[] subs = filepath.Split('\\');
                string filename = subs[subs.Length - 1];
                ProgressWindow progress = new ProgressWindow();
                progress.WindowStartupLocation = WindowStartupLocation.Manual;
                progress.Left = this.Left + this.Width / 2 - progress.Width / 2;
                progress.Top = this.Top + this.Height / 2 - progress.Height / 2;
                
                progress.Show();
                this.socket.Upload(filepath, filename, progress);
                progress.Close();
            }
            else
            {
                MessageBox.Show("Please choose a legal file!");
            }
            System.Threading.Thread.Sleep(1000);
            this.Refresh_Grid();
        }

        private void Refresh_Grid()
        {
            gridFiles.Children.RemoveRange(0, int.MaxValue);
            string[] filenames = this.socket.Dir();
            int count = 0;
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (count == filenames.Length - 1)
                    {
                        break;
                    }
                    if (File.Exists(@".\files\" + filenames[count]))
                    {
                        this.DrawFile(i, j, 1, filenames[count]);
                    }
                    else
                    {
                        this.DrawFile(i, j, 2, filenames[count]);
                    }
                    count++;
                }
            }
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            string filename = mi.DataContext.ToString().Split('?')[0];
            ProgressWindow progress = new ProgressWindow();
            progress.WindowStartupLocation = WindowStartupLocation.Manual;
            progress.Left = this.Left + this.Width / 2 - progress.Width / 2;
            progress.Top = this.Top + this.Height / 2 - progress.Height / 2;
            progress.Show();
            this.socket.Get(filename, progress);
            progress.Close();
            MessageBox.Show("Download finished!");
            this.Refresh_Grid();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            string filename = mi.DataContext.ToString().Split('?')[0];
            System.Diagnostics.Process.Start(@".\files\" + filename);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            string filename = mi.DataContext.ToString().Split('?')[0];
            bool isRemoved = SocketService.getInstance().Remove(filename);
            if (isRemoved)
            {
                MessageBox.Show(filename + " has been removed.");
            } else
            {
                MessageBox.Show("Cannot remove " + filename + "!");
            }
            this.Refresh_Grid();
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            string filename = mi.DataContext.ToString().Split('?')[0];
            string[] stats = new string[2];
            stats = SocketService.getInstance().Detail(filename);
            FileDetailWindow detailWindow = new FileDetailWindow();
            detailWindow.labelFilename.Content = filename;
            detailWindow.labelTime.Content = stats[0];
            detailWindow.labelSize.Content = GetReadableSize(int.Parse(stats[1]));
            detailWindow.labelBelong.Content = stats[2];
            detailWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            detailWindow.Left = this.Left + this.Width / 2 - detailWindow.Width / 2;
            detailWindow.Top = this.Top + this.Height / 2 - detailWindow.Height / 2;
            detailWindow.Show();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            this.Refresh_Grid();
        }

        private string GetReadableSize(int size)
        {
            if (size < 1024)
            {
                return size.ToString() + "B";
            } else if (size < 1024 * 1024)
            {
                return (size / 1024).ToString() + "KB";
            } else
            {
                return (size / 1024 / 1024).ToString() + "MB";
            }
        }
    }
}
