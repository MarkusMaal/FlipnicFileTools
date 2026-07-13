using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FlipnicLib;
using FlipnicLib.Formats;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI;

/// <summary>
/// Mock objects for Avalonia Designer previews
/// </summary>
public abstract class Mocks
{
    private static FpnFpc CameraObject { get; } = new (new MemoryStream([
        0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x70, 0x41, 0x00, 0x00, 0xF0, 0x41, 0x00, 0x00, 0xA1, 0x42, 0x00, 0x00, 0xB4, 0x42, 0x00, 0x80, 0xC8, 0x42,
        0x00, 0x80, 0xA1, 0x42, 0xCD, 0xCC, 0xF0, 0x41, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x00,
        0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0xA0, 0x40,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA0, 0xC0
    ]));

    private static FpnCol CollisionObject { get; } = new(true);

    public static void DisplayMocks(MainWindow mw)
    {
        mw.PreviewImage.Source = new Bitmap(StaticUtils.GenerateCheckerboardPng(320, 240));
        mw.ModelGrid.Background = new SolidColorBrush(Colors.Transparent);
        mw.InfoBox.Text = "This is where the information about currently opened file will be displayed";
        mw.EventBox.Text = "This is where the event script will be displayed";
        mw.CameraTool.CameraObject = CameraObject;
        mw.ColMap.ColObject = CollisionObject;
        mw.FilesGrid.ItemsSource = new ObservableCollection<VirtualFile>([
            new VirtualFile("SAMPLE.TXT", 0L, 0x800L, 0x40, true),
            new VirtualFile("DUMMY.DAT", 0x800L, 0x800L, 0x60, true),
        ]);
        mw.SamplesGrid.ItemsSource = new ObservableCollection<SampleColl>([
            new SampleColl
            {
                Data = null,
                Id = 0,
                LoopEnd = 0,
                LoopStart = 0,
                Offset = 0
            },
            new SampleColl
            {
                Data = null,
                Id = 1,
                LoopEnd = 722,
                LoopStart = 400,
                Offset = 0
            },
        ]);
        mw.StageLayoutsControl.LayoutSource = new ObservableCollection<FpnLay.Layout>([
            new FpnLay.Layout
            {
                Label = "SAMPLE_LAYOUT",
                PositionX = 0,
                PositionY = 0,
                PositionZ = 0,
                SizeX =  0,
                SizeY = 0,
                SizeZ = 0,
                SkewX =  0,
                SkewY = 0,
                SkewZ = 0,
            }
        ]);
        mw.RankingCombobox.SelectedIndex = 0;
        mw.GimmickCombobox.ItemsSource = new ObservableCollection<string>([
            "GMK_SAMPLE"
        ]);
        mw.GimmickCombobox.SelectedIndex = 0;
        mw.GimmickGrid.ItemsSource = new ObservableCollection<Gimmick>([
            new Gimmick
            {
                AnalogRange = 0,
                Bounciness = 0,
                Button = FormatBase.ControllerButtons.Disabled,
                FlipperStrength = 0,
                Invisible = false,
                Knockback = 0,
                Label = "DUMMY",
                NoSpawn = false,
                SoundEffect = 0,
                Type = Gimmick.GimmickTypes.HittableObject
            }
        ]);
    }
}