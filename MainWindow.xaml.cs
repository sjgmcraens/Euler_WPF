using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using ExtensionMethods;
using Problems;
using WpfMath;


namespace Euler_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Page ProblemSelectionPage = new ProblemSelectionPage();

        UserProfile currentUserProfile;

        string clientVersion = "1.0";


        public MainWindow()
        {
            InitializeComponent();

            // Create DefaultUser if it doesn't exist
            try
            {
                // Try loading default profile
                currentUserProfile = ProblemData.LoadUserData("DefaultUser" + ".xml");

                // Check loaded profile for version
                if (currentUserProfile.clientVersion != clientVersion)
                {
                    throw new Exception($"Loaded file (ver. {currentUserProfile.clientVersion}) has version mismatch with client (ver. {clientVersion})");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // Create new and save
                currentUserProfile = new UserProfile("DefaultUser", clientVersion);
                currentUserProfile.SaveUserData();
            }
        }

        #region Misc WPF functions
        public static Grid IntArrayToGrid(int[] x)
        {
            Grid g = new Grid() { Margin = new Thickness(10, 10, 10, 10) };

            // 2 rows
            g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            for (int i = 0; i <= x.Length; i++)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                // Text
                if (i == 0)
                {
                    TextBlock tB = new TextBlock() { Text = "Index:", Margin = new Thickness(10, 10, 10, 10) };
                    Grid.SetRow(tB, 0);
                    Grid.SetColumn(tB, i);
                    g.Children.Add(tB);

                    tB = new TextBlock() { Text = $"n:", Margin = new Thickness(10, 10, 10, 10) };
                    Grid.SetRow(tB, 1);
                    Grid.SetColumn(tB, i);
                    g.Children.Add(tB);
                }
                else
                {
                    TextBlock tB = new TextBlock() { Text = $"{i - 1}", Margin = new Thickness(10, 10, 10, 10) };
                    Grid.SetRow(tB, 0);
                    Grid.SetColumn(tB, i);
                    g.Children.Add(tB);

                    tB = new TextBlock() { Text = $"{x[i - 1]}", Margin = new Thickness(10, 10, 10, 10) };
                    Grid.SetRow(tB, 1);
                    Grid.SetColumn(tB, i);
                    g.Children.Add(tB);
                }



                // Rects
                Rectangle r = new Rectangle();
                r.Fill = new SolidColorBrush(Colors.Transparent);
                r.Stroke = new SolidColorBrush(Colors.Black);
                Grid.SetRow(r, 0);
                Grid.SetColumn(r, i);
                g.Children.Add(r);

                r = new Rectangle();
                r.Fill = new SolidColorBrush(Colors.Transparent);
                r.Stroke = new SolidColorBrush(Colors.Black);
                Grid.SetRow(r, 1);
                Grid.SetColumn(r, i);
                g.Children.Add(r);
            }

            return g;
        }
        public static Grid String2DArrayToGrid(string[,] s)
        {
            /// Dim 0 == X == ColI
            /// Dim 1 == Y == RowI

            Grid g = new Grid() { Margin = new Thickness(10, 10, 10, 10) };

            for (int i0 = 0; i0 < s.GetLength(0); i0++)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            }
            for (int i1 = 0; i1 < s.GetLength(1); i1++)
            {
                g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                for (int i0 = 0; i0 < s.GetLength(0); i0++)
                {
                    // Text
                    TextBlock tB = new TextBlock() { Text = s[i0, i1], Margin = new Thickness(10, 10, 10, 10) };
                    Grid.SetRow(tB, i1);
                    Grid.SetColumn(tB, i0);
                    g.Children.Add(tB);

                    // Outline rect
                    Rectangle r = new Rectangle()
                    {
                        Fill = new SolidColorBrush(Colors.Transparent),
                        Stroke = new SolidColorBrush(Colors.Black)
                    };
                    Grid.SetRow(r, i1);
                    Grid.SetColumn(r, i0);
                    g.Children.Add(r);
                }
            }
            return g;
        }
        #endregion

        //#region WPF Interaction functions
        //private void ProblemListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    // If nothing is selected, set the currentProblemGrid to display the default message
        //    if (ProblemListBox.SelectedItem == null)
        //    {
        //        SetCurrentProblem(-1);
        //    } else
        //    {
        //        SetCurrentProblem(Problems.Problems.P.Keys.ToList()[ProblemListBox.SelectedIndex]);
        //    }
        //}
        //private void SetCurrentProblem(int pIndex)
        //{
        //    // Remove solution discription
        //    RemoveSolutionDiscription();

        //    if (pIndex == -1)
        //    {
        //        CurrentProblemTitleBlock.Text = "Welcome to Project Euler";
        //        CurrentProblemDiscriptionBlock.Text = "Select any problem to view it's contents.";
        //        GetSolutionButton.Visibility = Visibility.Hidden;
        //    } else
        //    {
        //        CurrentProblemTitleBlock.Text = Problems.Problems.P[pIndex].Title;
        //        CurrentProblemDiscriptionBlock.Text = Problems.Problems.P[pIndex].Discription;
        //        CurrentProblemSolutionBlock.Text = "";

        //        GetSolutionButton.Visibility = Visibility.Visible;
        //        GetSolutionButton.IsEnabled = true;
        //    }
        //    //CurrentProblemSolutionBlock.Visibility = Visibility.Hidden;
        //    //CurrentProblemSolutionExpantionSP.Visibility = Visibility.Hidden;
        //}
        //private void GetSolutionButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // If something selected 
        //    if (ProblemListBox.SelectedItem != null)
        //    {
                
        //        Object[] solution = Problems.Problems.P[Problems.Problems.P.Keys.ToList()[ProblemListBox.SelectedIndex]].Solution();

        //        CurrentProblemSolutionBlock.Text = solution[0] + $" ({solution[1]}ms)";

        //        CurProbSP.Children.Add( (StackPanel)solution[2] );

        //        // Disable solution button
        //        GetSolutionButton.IsEnabled = false;
        //    }
        //}
        //private void RemoveSolutionDiscription()
        //{
        //    if (CurProbSP.Children.Count > 3)
        //    {
        //        CurProbSP.Children.RemoveAt(3);
        //    }
        //}
        //#endregion

        private void GoToProblemSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            this.Content = ProblemSelectionPage;
        }
    }
}

