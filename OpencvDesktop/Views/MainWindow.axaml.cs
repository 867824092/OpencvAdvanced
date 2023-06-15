using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpencvDesktop.ViewModels;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Window = Avalonia.Controls.Window;

namespace OpencvDesktop.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? VM => DataContext as MainWindowViewModel;
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Btn_select_img_OnClick(object? sender, RoutedEventArgs e) {
        var filePickerOpenOptions = new OpenFileDialog()
        {
            AllowMultiple = false,
        Title = "选择图片",
            Filters = new List<FileDialogFilter>()
            {
                new FileDialogFilter()
                {
                    Name = "图片",
                    Extensions = new List<string>() {"jpg", "png", "jpeg"}
                }
            }
        };
        var result = await filePickerOpenOptions.ShowAsync(this);
        if (result?.Length == 0) return;
        VM!.ImagePath =result[0];
    }
}