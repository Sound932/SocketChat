﻿using System;
using System.Windows;
using Online_Chat.ViewModels;
using Online_Chat.Events;
using System.Net.Sockets;
using System.Net;


namespace Online_Chat.Views
{
    /// <summary>
    /// Interaction logic for MainChat.xaml
    /// </summary>
    public partial class MainChat : Window
    {
        public MainChat(HomeViewModel Home)
        {
            InitializeComponent();
            DataContext = new ChatViewModel(Home.Client, Home.User);
        }
        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == e.LeftButton)
            {
                this.DragMove();
            }
        }

        private void MessageRequest(object sender, MessageEventArgs e)
        {
            MessageBox.Show(e.Message);
        }
    }
}