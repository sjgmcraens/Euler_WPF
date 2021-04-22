using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Serialization;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Problems
{
    /// <summary>
    ///  This namespace contains all the information about the problems.
    ///  Each problem is organised into it's own class.
    ///  The problem class template contains common variables and functions.
    /// </summary>

    // The Problem class contains everything relevant to one problem
    class Problem
    {
        #region Variables

        // Title and discription are set from initializer
        public string Title { get; set; }
        public string WebData { get; set; }

        // Other info is taken from a tooltip on the problem's website
        public string miscInfo; // Full tooltip
        public int difficulty;  // Taken from miscInfo
        public int solvedBy;    // Taken from miscInfo

        // Solution might be stored here someday
        public bool solutionKnown = false;

        // The description control is allways a stackpanel and is generated during loading.
        private FlowDocumentScrollViewer Description;

        #endregion
        #region Functions

        // Constructor
        public Problem(string _miscInfo)
        {
            // Get some info
            miscInfo = _miscInfo.Replace("<br>", " ");
            solvedBy = Convert.ToInt32(miscInfo.GetBetween("Solved by ", ";"));
            difficulty = Convert.ToInt32(miscInfo.GetBetween("Difficulty rating: ", "%"));
        }

        // Get FlowDoc of description
        public FlowDocumentScrollViewer GetDescriptionFDSV(int pNum)
        {
            if (Description is null)
            {
                #region FlowDoc preparation

                // Flowdoc to hold content
                FlowDocument FD = new FlowDocument()
                {
                    IsHyphenationEnabled = true,
                    IsOptimalParagraphEnabled = true,
                    FontFamily = new FontFamily("Segoe UI")
                };

                // Title
                TextBlock TitleTB = new TextBlock()
                {
                    FontSize = 30,
                    FontFamily = new FontFamily("Georgia"),
                };

                // Get link
                string linkPath = "https://projecteuler.net/problem=" + pNum;

                // Add hyperlink
                var HL = new Hyperlink(new Run($"{pNum}: " + Title))
                {
                    Foreground = new SolidColorBrush(Colors.Black),
                    TextDecorations = null,
                    NavigateUri = new Uri(linkPath)
                };
                HL.RequestNavigate += (sender, e) =>
                {
                    System.Diagnostics.Process.Start(e.Uri.ToString());
                };
                TitleTB.Inlines.Add(HL);

                FD.Blocks.Add(new BlockUIContainer()
                {
                    Child = TitleTB,
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(0,0,0,1),
                });

                // Subtext
                FD.Blocks.Add(new BlockUIContainer()
                {
                    Child = new TextBlock()
                    {
                        Text = miscInfo,
                        FontSize = 10,
                        Margin = new Thickness(0, 0, 0, 30),
                        TextWrapping = TextWrapping.Wrap
                    }
                });

                #endregion

                #region Description content

                #region Content prep

                // Get content
                string openingTag = "<div class=\"problem_content\" role=\"problem\">";
                int indexOfFirstDiv = WebData.IndexOf(openingTag);
                int indexOfFirstDivClosingTag;
                int depth = 0;
                int curIndex = indexOfFirstDiv;
                string curCode = "<div";
                while (true)
                {
                    // Find next code
                    var firstCodeIndices = new string[] { "<div", "</div" }.Select(code => (code, WebData.IndexOf(code, curIndex + curCode.Length))).Where(x => x.Item2 != -1);
                    var firstCode = firstCodeIndices.Where(x => x.Item2 == firstCodeIndices.Min(y => y.Item2)).First();
                    curIndex = firstCode.Item2;
                    curCode = firstCode.code;

                    if (firstCode.code == "<div")
                    {
                        depth++;
                    }
                    else
                    {
                        if (depth > 0)
                        {
                            depth--;
                        }
                        else
                        {
                            // Done
                            indexOfFirstDivClosingTag = firstCode.Item2;
                            break;
                        }
                    }
                }

                string pContent = WebData.Substring(indexOfFirstDiv + openingTag.Length, indexOfFirstDivClosingTag - indexOfFirstDiv - openingTag.Length);

                #endregion

                // Format Lines
                Block B;
                (pContent, B) = HTML.FormatFirstBlock(pContent);
                while (pContent.Length > 0)
                {
                    FD.Blocks.Add(B);
                    (pContent, B) = HTML.FormatFirstBlock(pContent);
                }

                #endregion

                Description = new FlowDocumentScrollViewer()
                {
                    Document = FD,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
            }
            return Description;
        }

        #endregion
    }

    // Class dealing with HTML decoding
    public static class HTML
    {
        // Paragraph tags
        public static readonly (string start, string end)[] blockTags = new (string start, string end)[]
        {
            ("<p", "</p>"),
            ("$$", "$$"),
            ("<div","</div>"),
            ("<blockquote", "</blockquote>"),
            ("<ul", "</ul>")
        };

        // Inline tag chars
        public static readonly char[] inlineTagChars = new char[]
        {
            '<',
            '$'
        };

        // Replace exit codes that can be replaced at the start of the parsing
        public static string PreparationReplace(string str)
        {
            new (string c, string r)[]
            {
                ("\n", ""),
                (" (right click and 'Save Link/Target As...')", "")
            }
            .ToList().ForEach(x => str = str.Replace(x.c, x.r));

            return str;
        }

        // Create formula control from string
        public static InlineUIContainer GetFormulaControl(string str)
        { 
            var C = new InlineUIContainer()
            {
                Child = new WpfMath.Controls.FormulaControl()
                {
                    Formula = str,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(5, 0, 5, 0)
                },
                BaselineAlignment = BaselineAlignment.Bottom,
            };

            return C;
        }

        // Replace exit codes that should only be replaced at the VERY END of the parsing
        public static Run GetFinishedRun(string str)
        {
            (string c, string r) code = ("&lt;", "<");
            return new Run(str.Replace(code.c, code.r));
        }

        public static (string, Block) FormatFirstBlock(string str)
        {
            char[] blockTagChars = blockTags.Select(x => x.start[0]).Distinct().ToArray();

            // Return empty string if str contains no blockTags
            if (!blockTags.Any(x => str.Contains(x.start)) )
            {
                return ("", new Paragraph());
            }

            // Get open tag index
            int openTagI = blockTags.Select(x => str.IndexOf(x.start)).Where(x => x != -1).Min();

            // Trim block
            str = str.Substring(openTagI);

            // Get first char of open tag
            char firstCharOfOpenTag = str[0];

            // Switch for first char of open tag (cases terminate)
            switch (firstCharOfOpenTag)
            {
                case '<':
                    {
                        // Get tag name
                        string tagName = blockTags.Where(x => str.IndexOf(x.start) == 0).First().start.Substring(1);

                        // Determine fullTag
                        string fullTag = str.GetBetweenInclusive("<", "</" + tagName + ">");

                        // Get open tag
                        string openTag = fullTag.GetBetweenInclusive("<", ">");

                        // Get tag content
                        string tagContent = fullTag.GetBetween(openTag, "</" + tagName + ">");

                        // Switch for tag name
                        switch (tagName)
                        {
                            case "blockquote": // Indented paragraph
                            case "p": // Paragraph
                            case "div": // Section (same as paragraph as far as i can see)
                                {
                                    // Create paragraph
                                    Paragraph P = new Paragraph();

                                    // Indent if blockquote
                                    if (tagName == "blockquote")
                                    {
                                        P.Padding = new Thickness(50, 0, 0, 0);
                                    }

                                    // Class="class"
                                    if (openTag.Contains("class="))
                                    {
                                        // Switch for class
                                        switch (openTag.GetBetween("class=\"", "\""))
                                        {
                                            // Centered
                                            case "center":
                                                {
                                                    P.TextAlignment = TextAlignment.Center;
                                                    break;
                                                }
                                            // Monospaced centered
                                            case "monospace center":
                                                {
                                                    // Centered
                                                    P.TextAlignment = TextAlignment.Center;
                                                    // Monospaced font
                                                    P.FontFamily = new FontFamily("Courier New");
                                                    // Reduced fontsize
                                                    //P.FontSize *= 0.9;
                                                    // Remove linespacing
                                                    P.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                                                    P.LineHeight = P.FontSize + 3;
                                                    // Special padding
                                                    //P.Padding = new Thickness(0, 0, 0, 0);
                                                    break;
                                                }
                                            // Monospaced
                                            case "monospace":
                                                {
                                                    // Monospaced font
                                                    P.FontFamily = new FontFamily("Courier New");
                                                    // Reduced fontsize
                                                    //P.FontSize *= 0.75;
                                                    // Remove linespacing
                                                    P.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                                                    P.LineHeight = P.FontSize + 3;
                                                    // Special padding
                                                    P.Padding = new Thickness(50, 0, 0, 0);
                                                    break;
                                                }
                                            // Left margin
                                            case "margin_left":
                                                {
                                                    P.Padding = new Thickness(50, 0, 0, 0);
                                                    break;
                                                }
                                            // Note (nothing)
                                            case "note":
                                                {
                                                    break;
                                                }
                                            // Smaller
                                            case "smaller":
                                                {
                                                    P.FontSize *= 0.9;
                                                    break;
                                                }
                                            // Unknown
                                            default:
                                                {
                                                    throw new Exception($"Unkown class in: {str}");
                                                }
                                        }
                                    }

                                    // Format content and add to P
                                    FormatLine(tagContent).ToList().ForEach(x => P.Inlines.Add(x));

                                    // Return
                                    return (str.Substring(fullTag.Length), P);
                                }
                            case "ul":
                                {
                                    // Create list
                                    var contentList = new List
                                    {
                                        MarkerOffset = 10,
                                        MarkerStyle = TextMarkerStyle.Disc,
                                        Margin = new Thickness(30, 10, 10, 10)
                                    };

                                    // Put all <li> items into list
                                    while (tagContent.Contains("<li>"))
                                    {
                                        int liCloseIndex = tagContent.IndexOf("</li>");

                                        string liItem = tagContent.GetBetween("<li>", "</li>");
                                        tagContent = tagContent.Remove(0, liCloseIndex + "</li>".Length);

                                        var Par = new Paragraph();
                                        FormatLine(liItem.Replace("<br />", "")).ForEach(x => Par.Inlines.Add(x));

                                        var lItem = new ListItem(Par);
                                        contentList.ListItems.Add(lItem);
                                    }

                                    // Add block to FD
                                    return (str.Substring(fullTag.Length), contentList);
                                }
                            default:
                                {
                                    throw new Exception($"Unknown blockTag tagName: {tagName}");
                                }
                        }
                    }
                case '$':
                    {
                        // Get full tag
                        string fullTag = str.GetBetweenInclusive("$$", "$$");
                        // Create centered paragraph
                        Paragraph P = new Paragraph()
                        {
                            TextAlignment = TextAlignment.Center
                        };
                        // Add formula to P
                        P.Inlines.Add(GetFormulaControl(fullTag.GetBetween("$$","$$")));
                        // Return
                        return (str.Substring(fullTag.Length), P);
                    }
                default:
                    {
                        throw new Exception($"Unknown block tag: {str}");
                    }
            }
        }

        // Return the string as a list of Inlines
        public static List<Inline> FormatLine(string line)
        {
            /// This function processes the html code string "line" into a list of WPF Inline's.
            /// Inlines can hold pretty much anything, including images
            /// The function processes one tag, and then calls itself to format the remainder string
            /// and concatonates the result of the remainder to the Inline list.

            // Create inline list
            List<Inline> inlines = new List<Inline>();

            // Do prep replace
            line = PreparationReplace(line);

            // If line contains no tags ==> return line as Inline
            if (!(inlineTagChars.Any(x => line.Contains(x))))
            {
                inlines.Add(GetFinishedRun(line));
                return inlines;
            }

            // Line contains tags ==> get index of first tag
            (int index, char firstChar) tag;
            tag.index = inlineTagChars.Select(x => line.IndexOf(x)).Where(x => x != -1).Min();
            tag.firstChar = line[tag.index];

            // Format part before first escape and add to inlines
            // Also remove beforePart from line, now tag.index should be 0
            if (tag.index > 0)
            {
                string beforePart = line.Substring(0, tag.index);
                inlines.Add(GetFinishedRun(beforePart));
                line = line.Remove(0, tag.index);
                tag.index = 0;
            }

            // The complete tag string is determined separately, but gets removed after the operations
            string fullTag;

            // Switch for first char (cases must determine fullTag)
            switch (tag.firstChar)
            {
                case '$':
                    {
                        // Inline formula
                        fullTag = line.GetBetweenInclusive("$", "$");

                        // Add inline of formula
                        inlines.Add(GetFormulaControl(fullTag.GetBetween("$", "$")));

                        break;
                    }
                case '<':
                    {
                        // Get open tag
                        string openTag = line.GetBetweenInclusive("<",">");

                        // Switch for if tag is container
                        switch (openTag.EndsWith("/>"))
                        {
                            // Non-container tag
                            case true:
                                {
                                    // Set fullTag
                                    fullTag = openTag;

                                    // Get tag name
                                    string tagName = openTag.Substring(1, new string[]
                                    {
                                        openTag.GetBetween("<", " "), openTag.GetBetween("<", "/>")
                                    }.Min(x => x.Length));

                                    // Switch for tag name
                                    switch (tagName)
                                    {
                                        // Linebreak
                                        case "br":
                                            {
                                                if (line.Substring(fullTag.Length) != "")
                                                {
                                                    inlines.Add(new Run("\n"));
                                                }
                                                
                                                break;
                                            }
                                        // Image
                                        case "img":
                                            {
                                                // Content
                                                string imagePath = "https://projecteuler.net/" + fullTag.GetBetween("\"", "\"");

                                                // Get image
                                                BitmapImage BMI = new BitmapImage(new Uri(imagePath));

                                                Image img = new Image()
                                                {
                                                    Stretch = Stretch.Uniform
                                                };

                                                BMI.DownloadCompleted += delegate (object sender, EventArgs e)
                                                {
                                                    img.Source = BMI;
                                                    img.Width = BMI.Width * 2;
                                                    img.Height = BMI.Height * 2;
                                                };

                                                // Add content
                                                inlines.Add(new InlineUIContainer()
                                                {
                                                    Child = img
                                                });

                                                break;
                                            }
                                        default:
                                            {
                                                throw new Exception($"Unknown non-container tag: {fullTag}");
                                            }
                                    }

                                    break;
                                }
                            // Container tag
                            case false:
                                {
                                    // Get tag name
                                    string tagName = openTag.Substring(1, new string[]
                                    {
                                        openTag.GetBetween("<", " "), openTag.GetBetween("<", ">")
                                    }.Where(x => x.Length > 0).Min(x => x.Length));

                                    // Set fullTag
                                    fullTag = line.GetBetweenInclusive(openTag, "</" + tagName + ">");

                                    //// Throw error if tag contains tag with identical name
                                    //if (fullTag.Contains("<" + tagName))
                                    //{
                                    //    throw new Exception($"Tag: {fullTag} contains tag with same tagName: {tagName}");
                                    //}

                                    // Get tag content
                                    string tagContent = fullTag.GetBetween(openTag, "</" + tagName + ">");

                                    // Switch for tag name (known container tags)
                                    switch (tagName)
                                    {
                                        case "a": // Hyperlink
                                            {
                                                // Get link
                                                string linkPath = "https://projecteuler.net/" + fullTag.GetBetween("href=\"", "\"");

                                                // Add hyperlink
                                                var HL = new Hyperlink(GetFinishedRun(tagContent))
                                                {
                                                    NavigateUri = new Uri(linkPath)
                                                };
                                                HL.RequestNavigate += (sender, e) =>
                                                {
                                                    System.Diagnostics.Process.Start(e.Uri.ToString());
                                                };
                                                inlines.Add(HL);

                                                break;
                                            }

                                        case "b": // Bold text
                                            {
                                                // Add content
                                                inlines.Add(new Bold(GetFinishedRun(tagContent)));

                                                break;
                                            }

                                        case "dfn": // Italic text with tooltip
                                            {
                                                // Get tooltip
                                                string tooltip = fullTag.GetBetween("title=\"", "\"");

                                                // Add italic part with tooltip
                                                inlines.Add(new Italic(GetFinishedRun(tagContent))
                                                {
                                                    ToolTip = new ToolTip()
                                                    {
                                                        Content = $" ({tooltip})"
                                                    }
                                                });

                                                break;
                                            }

                                        case "sub": // Subscript
                                            {
                                                // Convert content to subscript
                                                var content = FormatLine(tagContent);
                                                content.ForEach(x => x.BaselineAlignment = BaselineAlignment.Subscript);
                                                content.ForEach(x => x.FontSize *= 0.75);

                                                //content.ForEach(x => Typography.SetVariants(x, FontVariants.Subscript));

                                                // Add content
                                                content.ForEach(x => inlines.Add(x));

                                                break;
                                            }

                                        case "sup": // Superscript
                                            {
                                                // Convert content to subscript
                                                var content = FormatLine(tagContent);
                                                content.ForEach(x => x.BaselineAlignment = BaselineAlignment.TextTop);
                                                content.ForEach(x => x.FontSize *= 0.75);

                                                //content.ForEach(x => Typography.SetVariants(x, FontVariants.Superscript));

                                                // Add content
                                                content.ForEach(x => inlines.Add(x));

                                                break;
                                            }

                                        case "span": // Color
                                            {
                                                // Getting color
                                                string color = fullTag.GetBetween("<span class=\"", "\"");

                                                // Get content inlines
                                                List<Inline> contentInlines = FormatLine(tagContent);

                                                // Switch for colors
                                                switch (color) // Terminates
                                                {
                                                    case "red":
                                                        {
                                                            
                                                            // Apply red
                                                            contentInlines.ForEach(x => x.Foreground = new SolidColorBrush(Colors.Red));
                                                        }
                                                        break;
                                                    case "red strong":
                                                        {
                                                            // Apply red and bold
                                                            contentInlines.ForEach(x => x.Foreground = new SolidColorBrush(Colors.Red));
                                                            contentInlines.ForEach(x => x.FontWeight = FontWeights.Bold);
                                                            break;
                                                        }
                                                    default: // Unknown
                                                        {
                                                            throw new Exception($"Unknown span color: {color}");
                                                        }
                                                }

                                                // Put inlines into inlines
                                                inlines = inlines.Concat(contentInlines).ToList();

                                                break;
                                            }
                                        // Italic
                                        case "i":
                                        case "var":
                                            {
                                                // Add content as italic
                                                inlines.Add(new Italic(GetFinishedRun(tagContent)));

                                                break;
                                            }
                                        // Table
                                        case "table":
                                            {
                                                Grid G = new Grid();

                                                // Go over rows
                                                int curRow = 0;
                                                while (tagContent.Contains("<tr>"))
                                                {
                                                    int curCol = 0;
                                                    // Add row
                                                    G.RowDefinitions.Add(new RowDefinition());
                                                    // Get row
                                                    string rowStr = tagContent.GetBetweenInclusive("<tr>", "</tr>");
                                                    // Remove row
                                                    tagContent = tagContent.Remove(0, rowStr.Length);

                                                    while (rowStr.Contains("<td>"))
                                                    {
                                                        if (curCol >= G.ColumnDefinitions.Count)
                                                        {
                                                            G.ColumnDefinitions.Add(new ColumnDefinition());
                                                        }

                                                        string cellStr = rowStr.GetBetweenInclusive("<td>", "</td>");
                                                        rowStr = rowStr.Remove(0, cellStr.Length);
                                                        string cellStrContent = cellStr.GetBetween("<td>", "</td>");

                                                        TextBlock TB = new TextBlock();
                                                        FormatLine(cellStrContent).ForEach(x => TB.Inlines.Add(x));
                                                        Grid.SetColumn(TB, curCol);
                                                        Grid.SetRow(TB, curRow);
                                                        G.Children.Add(TB);

                                                        curCol++;
                                                    }
                                                    curRow++;
                                                }

                                                inlines.Add(new InlineUIContainer()
                                                {
                                                    Child = G
                                                });

                                                break;
                                            }
                                        // Section
                                        case "div":
                                            {
                                                FormatLine(tagContent).ForEach(x => inlines.Add(x));

                                                break;
                                            }
                                        default:
                                            {
                                                throw new Exception($"Unknown inline tag: {tagName}");
                                            }
                                    }

                                    break;
                                }
                            default:
                                {
                                    throw new Exception($"If you're reading this, there is something very wrong");
                                }
                        }

                        break;
                    }
                default:
                    {
                        throw new Exception($"No known escape char: {tag.firstChar}");
                    }
            }

            // Remove tag from line
            line = line.Remove(0, fullTag.Length);

            // Format remainder and add to list
            if (line != "")
            {
                FormatLine(line).ForEach(x => inlines.Add(x));
            }

            // Return
            return inlines;
        }
    }

    // Problem datastructure and loading functions
    static class ProblemData
    /// Static class containing the problem data
    {

        public static int totalProblemAmount, problemsLoaded;
        public static Dictionary<int, Problem> Problems;

        private static System.Net.WebClient webClient;

        private static string archivesWebData;

        static ProblemData()
        {
            #region Initialize variables

            // Set the amount of problems to load initially
            problemsLoaded = 0;

            // Create problem dictionairy
            Problems = new Dictionary<int, Problem>();

            #endregion

            #region Load problems

            // Create WebClient
            webClient = new System.Net.WebClient();

            // Get the number of problems
            archivesWebData = Encoding.UTF8.GetString( webClient.DownloadData("https://projecteuler.net/archives") );
            
            // Find "The problems archives table shows problems 1 to xxx."
            string totalProblemAmountString = archivesWebData.GetBetween("The problems archives table shows problems 1 to ", ".");
            totalProblemAmount = Convert.ToInt32(totalProblemAmountString);

            // Load the first 10 problems
            LoadNext(10);

            #endregion
        }

        public static void LoadNext(int loadAm)
        {
            // For each problem
            for (int pI = problemsLoaded; pI < problemsLoaded + loadAm; pI++)
            {
                // Get problem number
                int pN = pI + 1;
                // Get webData
                byte[] rawBytes = webClient.DownloadData("http://projecteuler.net/problem=" + pN);
                string webData = System.Text.Encoding.UTF8.GetString(rawBytes);


                // Add problem to dictionairy
                Problems.Add(pN, new Problem(webData.GetBetween("<span class=\"tooltiptext_right\">", "</span>"))
                {
                    Title = webData.GetBetween("<h2>", "</h2>"),
                    WebData = webData
                }) ;

            }
            problemsLoaded += loadAm;
        }

        public static void LoadAtLeast(int amount)
        {
            while (Problems.Count < amount)
            {
                LoadNext(1);
            }
        }

        public static UserProfile LoadUserData(string userDataFileName)
        {
            using (FileStream fs = new FileStream(userDataFileName, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(UserProfile));
                return (UserProfile)serializer.Deserialize(fs);
            }
        }
    }

    // Class structure for user profiles (xml storage)
    [Serializable]
    public class UserProfile 
    {

        public string userName;
        public string userDataFileName;
        public List<KeyValuePair<int, string>> userProblemState;
        public string clientVersion;

        // Default constructor for serialization
        public UserProfile() { }

        // Actual contructor
        public UserProfile( string _userName, string _clientVersion ) 
        {
            userName = _userName;
            userDataFileName = userName + "_Data.xml";
            userProblemState = new List<KeyValuePair<int, string>>();
            clientVersion = _clientVersion;
        }

        public void SaveUserData()
        {
            using (FileStream fs = new FileStream(userDataFileName, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(UserProfile));
                serializer.Serialize(fs, this);
            }
        }
    }


    //class P_1 // Multiples of 3 and 5
    //{
    //    string Title = "Multiples of 3 and 5";
    //    int Difficulty = 5;
    //    int Solved = 964994;
    //    StackPanel Discription()
    //    {
    //        StackPanel SP = new StackPanel();

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "If we list all the natural numbers below 10 that are multiples of 3 or 5, we get 3, 5, 6 and 9. The sum of these multiples is 23."
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "Find the sum of all the multiples of 3 or 5 below 1000."
    //        });

    //        return SP;
    //    }
    //    long Solution()
    //    {
    //        // Find the sum of all the multiples of 3 or 5 below 1000.
    //        int upperLimit = 1000;
    //        int sum = 0;
    //        int[] divisors = new int[] { 3, 5 };

    //        foreach (int d in divisors)
    //        {
    //            int m = 1;
    //            for (int prod = m * d; prod < upperLimit; prod = m * d)
    //            {
    //                sum += prod;
    //                m++;
    //            }
    //        }
    //        return sum;
    //    }
    //}

    //class P_2 // Even Fibonacci numbers
    //{
    //    string Title = "Even Fibonacci numbers";
    //    int Difficulty = 5;
    //    int Solved = 768279;
    //    StackPanel Discription()
    //    {
    //        StackPanel SP = new StackPanel();

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "Each new term in the Fibonacci sequence is generated by adding the previous two terms. By starting with 1 and 2, the first 10 terms will be:"
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "1, 2, 3, 5, 8, 13, 21, 34, 55, 89, ...",
    //            HorizontalAlignment = HorizontalAlignment.Center
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "By considering the terms in the Fibonacci sequence whose values do not exceed four million, find the sum of the even-valued terms."
    //        });

    //        return SP;
    //    }
    //    long Solution()
    //    {
    //        // By considering the terms in the Fibonacci sequence whose values do not exceed four million, find the sum of the even-valued terms.
    //        List<int> Fib = new List<int>() { 1, 2 };
    //        int upperLimit = 4000000;
    //        long sum = 2;
    //        for (int nextFib = Fib[Fib.Count - 1] + Fib[Fib.Count - 2]; 
    //            nextFib < upperLimit; 
    //            nextFib = Fib[Fib.Count - 1] + Fib[Fib.Count - 2])
    //        {
    //            Fib.Add(nextFib);
    //            if (nextFib % 2 == 0) // If even
    //            {
    //                sum += nextFib;
    //            }
    //        }
    //        return sum;
    //    }
    //}

    //class P_3 // Largest prime factor
    //{
    //    string Title = "Largest prime factor";
    //    int Difficulty = 5;
    //    int Solved = 550872;
    //    StackPanel Discription()
    //    {
    //        StackPanel SP = new StackPanel();

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "The prime factors of 13195 are 5, 7, 13 and 29."
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "What is the largest prime factor of the number 600851475143 ?"
    //        });

    //        return SP;
    //    }
    //    long Solution()
    //    {
    //        // What is the largest prime factor of the number 600851475143 ?
    //        long numToFactor = 600851475143;

    //        // Get factors
    //        Dictionary<int, int> factors = Prime.GetPrimeFactors(numToFactor);
    //        return factors.Keys.Max();
    //    }
    //}

    //class P_4 // Largest palindrome product
    //{
    //    string Title = "Largest palindrome product";
    //    int Difficulty = 5;
    //    int Solved = 487618;
    //    StackPanel Discription()
    //    {
    //        StackPanel SP = new StackPanel();

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "A palindromic number reads the same both ways. The largest palindrome made from the product of two 2-digit numbers is 9009 = 91 × 99."
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "Find the largest palindrome made from the product of two 3-digit numbers."
    //        });

    //        return SP;
    //    }
    //    long Solution()
    //    {
    //        // Find the largest palindrome made from the product of two 3-digit numbers.

    //        // Settings
    //        int Multdigits = 3;
    //        int Multamount = 2; // Doesn't work for inputs > 2 unfortunately.

    //        // Some preparatory calculations.
    //        int maxMult = Int32.Parse(String.Concat(Enumerable.Repeat("9", Multdigits))); // 999
    //        int maxTotal = Convert.ToInt32(Math.Pow(maxMult, Multamount)); // 999*999 = 998001

    //        int minMult = Convert.ToInt32(Math.Pow(10, Multdigits - 1)); // 100
    //        int minTotal = Convert.ToInt32(Math.Pow(minMult, Multamount)); // 100*100 = 100000

    //        // Determine first palindrome
    //        int palindrome = Functions.Misc.GetLargestPalinDromeBelow(maxTotal);

    //        while (palindrome >= minTotal)
    //        {
    //            // Now that we have a palindrome, lets check whether it can be made using two three-digit numbers
    //            for (int i = maxMult; i >= minMult; i--) // 999, 998, ... , 100
    //            {
    //                if (palindrome % i == 0) // If wholly divisible (to completely generalize: make this recursive)
    //                {
    //                    if ((palindrome / i).ToString().Length == Multdigits) // If the division creates a 3-digit number.
    //                    {
    //                        return palindrome; // Return the succesfull palindrome.
    //                    }
    //                }
    //            }
    //            palindrome = Functions.Misc.GetLargestPalinDromeBelow(palindrome - 1);
    //        }
    //        // No solution was found.
    //        return 0;
    //    }
    //}

    //class P_5 // Smallest multiple
    //{
    //    string Title = "Smallest multiple";
    //    int Difficulty = 5;
    //    int Solved = 491134;
    //    StackPanel Discription()
    //    {
    //        StackPanel SP = new StackPanel();

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "2520 is the smallest number that can be divided by each of the numbers from 1 to 10 without any remainder."
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "What is the smallest positive number that is evenly divisible by all of the numbers from 1 to 20?"
    //        });

    //        return SP;
    //    }
    //    long Solution()
    //    {
    //        // What is the smallest positive number that is evenly divisible by all of the numbers from 1 to 20?
    //        List<int> ns = new List<int>();
    //        for (int i = 1; i <= 20; i++)
    //        {
    //            ns.Add(i);
    //        }
    //        return Functions.Misc.LowestCommonMultiple(ns);
    //    }
    //}

    //class P_6 // Sum square difference
    //{
    //    string Title = "Sum square difference";
    //    int Difficulty = 5;
    //    int Solved = 494180;
    //    StackPanel Discription()
    //    {
    //        StackPanel SP = new StackPanel();

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "The sum of the squares of the first ten natural numbers is,"
    //        });

    //        SP.Children.Add(new WpfMath.Controls.FormulaControl()
    //        {
    //            Formula = @"1^2 + 2^2 + ... + 10^2 = 385",
    //            HorizontalAlignment = HorizontalAlignment.Center
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "The square of the sum of the first ten natural numbers is,"
    //        });

    //        SP.Children.Add(new WpfMath.Controls.FormulaControl()
    //        {
    //            Formula = @"(1 + 2 + ... + 10)^2 = 55^2 = 3025",
    //            HorizontalAlignment = HorizontalAlignment.Center
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "Hence the difference between the sum of the squares of the first ten natural numbers and the square of the sum is,"
    //        });

    //        SP.Children.Add(new WpfMath.Controls.FormulaControl()
    //        {
    //            Formula = @"3025 - 385 = 2640",
    //            HorizontalAlignment = HorizontalAlignment.Center
    //        });

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "Find the difference between the sum of the squares of the first one hundred natural numbers and the square of the sum."
    //        });

    //        return SP;
    //    }
    //    long Solution()
    //    {
    //        // Find the difference between the sum of the squares of the first one hundred natural numbers and the square of the sum.
    //        return something;
    //    }
    //}

    //class P_Template // Title here
    //{
    //    string Title = "";
    //    int Difficulty = 5;
    //    int Solved = 999999;
    //    StackPanel Discription()
    //    {
    //        StackPanel SP = new StackPanel();

    //        SP.Children.Add(new TextBlock()
    //        {
    //            Margin = new Thickness(10, 10, 10, 10),
    //            Text = "Something"
    //        });

    //        return SP;
    //    }
    //    long Solution()
    //    {
    //        // [Objective here]
    //        return something;
    //    }
    //}

    //static class Problems
    //{
    //    // This class contains the information pertaining to the all the problems.

    //    // Dictinairy of problems
    //    public static readonly Dictionary<int, Problem> P;
    //    private static System.Diagnostics.Stopwatch SW;

    //    static Problems()
    //    {
    //        SW = new System.Diagnostics.Stopwatch();

    //        P = new Dictionary<int, Problem>
    //            {
    //                {
    //                    1,
    //                    new Problem("Multiples of 3 and 5",
    //                    "If we list all the natural numbers below 10 that are multiples of 3 or 5, we get 3, 5, 6 and 9. The sum of these multiples is 23.\n" +
    //                    "Find the sum of all the multiples of 3 or 5 below 1000.",
    //                    964994, 5, Sol_1)
    //                },

    //                {
    //                    2,
    //                    new Problem("Even Fibonacci numbers",
    //                    "Each new term in the Fibonacci sequence is generated by adding the previous two terms. By starting with 1 and 2, the first 10 terms will be:\n" +
    //                    "1, 2, 3, 5, 8, 13, 21, 34, 55, 89, ...\n" +
    //                    "By considering the terms in the Fibonacci sequence whose values do not exceed four million, find the sum of the even-valued terms.",
    //                    768279, 5, Sol_2)
    //                },

    //                {
    //                    3,
    //                    new Problem("Largest prime factor",
    //                    "The prime factors of 13195 are 5, 7, 13 and 29.\n" +
    //                    "What is the largest prime factor of the number 600851475143 ?",
    //                    550872, 5, Sol_3)
    //                },

    //                {
    //                    4,
    //                    new Problem("Largest palindrome product",
    //                    "A palindromic number reads the same both ways. The largest palindrome made from the product of two 2-digit numbers is 9009 = 91 × 99.\n" +
    //                    "Find the largest palindrome made from the product of two 3-digit numbers.",
    //                    486673, 5, Sol_4)
    //                },

    //                {
    //                    5,
    //                    new Problem("Smallest multiple",
    //                    "2520 is the smallest number that can be divided by each of the numbers from 1 to 10 without any remainder.\n" +
    //                    "What is the smallest positive number that is evenly divisible by all of the numbers from 1 to 20?",
    //                    486673, 5, Sol_5)
    //                },

    //                {
    //                    6,
    //                    new Problem("Sum square difference",
    //                    "The sum of the squares of the first ten natural numbers is,\n" +
    //                    "1+4+9+...+100 = 385\n" +
    //                    "The square of the sum of the first ten natural numbers is,\n" +
    //                    "(1+2+3+...+10)^2 = 3025\n" +
    //                    "Hence the difference between the sum of the squares of the first ten natural numbers and the square of the sum is,\n" +
    //                    "3025-385 = 2640\n" +
    //                    "Find the difference between the sum of the squares of the first one hundred natural numbers and the square of the sum.",
    //                    493325, 5, Sol_6)
    //                },

    //                {
    //                    7,
    //                    new Problem("10001st prime",
    //                    "By listing the first six prime numbers: 2, 3, 5, 7, 11, and 13, we can see that the 6th prime is 13.\n" +
    //                    "What is the 10 001st prime number?",
    //                    421486, 5, Sol_7)
    //                },

    //                {
    //                    8,
    //                    new Problem("Largest product in a series",
    //                    "The four adjacent digits in the 1000-digit number that have the greatest product are 9 × 9 × 8 × 9 = 5832.,\n" +
    //                    "73167176531330624919225119674426574742355349194934\n" +
    //                    "96983520312774506326239578318016984801869478851843\n" +
    //                    "85861560789112949495459501737958331952853208805511\n" +
    //                    "12540698747158523863050715693290963295227443043557\n" +
    //                    "66896648950445244523161731856403098711121722383113\n" +
    //                    "62229893423380308135336276614282806444486645238749\n" +
    //                    "30358907296290491560440772390713810515859307960866\n" +
    //                    "70172427121883998797908792274921901699720888093776\n" +
    //                    "65727333001053367881220235421809751254540594752243\n" +
    //                    "52584907711670556013604839586446706324415722155397\n" +
    //                    "53697817977846174064955149290862569321978468622482\n" +
    //                    "83972241375657056057490261407972968652414535100474\n" +
    //                    "82166370484403199890008895243450658541227588666881\n" +
    //                    "16427171479924442928230863465674813919123162824586\n" +
    //                    "17866458359124566529476545682848912883142607690042\n" +
    //                    "24219022671055626321111109370544217506941658960408\n" +
    //                    "07198403850962455444362981230987879927244284909188\n" +
    //                    "84580156166097919133875499200524063689912560717606\n" +
    //                    "05886116467109405077541002256983155200055935729725\n" +
    //                    "71636269561882670428252483600823257530420752963450\n" +
    //                    "Find the thirteen adjacent digits in the 1000-digit number that have the greatest product. What is the value of this product?",
    //                    352346, 5, Sol_8)
    //                },

    //                {
    //                    9,
    //                    new Problem("Special Pythagorean triplet",
    //                    "A Pythagorean triplet is a set of three natural numbers, a < b < c, for which,\n" +
    //                    "a2 + b2 = c2\n" +
    //                    "For example, 32 + 42 = 9 + 16 = 25 = 52.\n" +
    //                    "There exists exactly one Pythagorean triplet for which a + b + c = 1000.\n" +
    //                    "Find the product abc.\n",
    //                    357757, 5, Sol_9)
    //                },

    //                {
    //                    10,
    //                    new Problem("Summation of primes",
    //                    "The sum of the primes below 10 is 2 + 3 + 5 + 7 = 17.\n" +
    //                    "Find the sum of all the primes below two million.\n",
    //                    327678, 5, Sol_10)
    //                },

    //                {
    //                    11,
    //                    new Problem("Largest product in a grid",
    //                    "In the 20×20 grid below, four numbers along a diagonal line have been marked in red.\n" +
    //                    "08 02 22 97 38 15 00 40 00 75 04 05 07 78 52 12 50 77 91 08\n" +
    //                    "49 49 99 40 17 81 18 57 60 87 17 40 98 43 69 48 04 56 62 00\n" +
    //                    "81 49 31 73 55 79 14 29 93 71 40 67 53 88 30 03 49 13 36 65\n" +
    //                    "52 70 95 23 04 60 11 42 69 24 68 56 01 32 56 71 37 02 36 91\n" +
    //                    "22 31 16 71 51 67 63 89 41 92 36 54 22 40 40 28 66 33 13 80\n" +
    //                    "24 47 32 60 99 03 45 02 44 75 33 53 78 36 84 20 35 17 12 50\n" +
    //                    "32 98 81 28 64 23 67 10 26 38 40 67 59 54 70 66 18 38 64 70\n" +
    //                    "67 26 20 68 02 62 12 20 95 63 94 39 63 08 40 91 66 49 94 21\n" +
    //                    "24 55 58 05 66 73 99 26 97 17 78 78 96 83 14 88 34 89 63 72\n" +
    //                    "21 36 23 09 75 00 76 44 20 45 35 14 00 61 33 97 34 31 33 95\n" +
    //                    "78 17 53 28 22 75 31 67 15 94 03 80 04 62 16 14 09 53 56 92\n" +
    //                    "16 39 05 42 96 35 31 47 55 58 88 24 00 17 54 24 36 29 85 57\n" +
    //                    "86 56 00 48 35 71 89 07 05 44 44 37 44 60 21 58 51 54 17 58\n" +
    //                    "19 80 81 68 05 94 47 69 28 73 92 13 86 52 17 77 04 89 55 40\n" +
    //                    "04 52 08 83 97 35 99 16 07 97 57 32 16 26 26 79 33 27 98 66\n" +
    //                    "88 36 68 87 57 62 20 72 03 46 33 67 46 55 12 32 63 93 53 69\n" +
    //                    "04 42 16 73 38 25 39 11 24 94 72 18 08 46 29 32 40 62 76 36\n" +
    //                    "20 69 36 41 72 30 23 88 34 62 99 69 82 67 59 85 74 04 36 16\n" +
    //                    "20 73 35 29 78 31 90 01 74 31 49 71 48 86 81 16 23 57 05 54\n" +
    //                    "01 70 54 71 83 51 54 69 16 92 33 48 61 43 52 01 89 19 67 48\n" +
    //                    "The product of these numbers is 26 × 63 × 78 × 14 = 1788696./n" +
    //                    "What is the greatest product of four adjacent numbers in the same direction (up, down, left, right, or diagonally) in the 20×20 grid?",
    //                    234222, 5, Sol_11)
    //                },

    //                {
    //                    12,
    //                    new Problem("Highly divisible triangular number",
    //                    "The sequence of triangle numbers is generated by adding the natural numbers. So the 7th triangle number would be 1 + 2 + 3 + 4 + 5 + 6 + 7 = 28. The first ten terms would be:\n" +
    //                    "1, 3, 6, 10, 15, 21, 28, 36, 45, 55, ...\n" +
    //                    "Let us list the factors of the first seven triangle numbers:\n" +
    //                    "1: 1\n" +
    //                    "3: 1,3\n" +
    //                    "6: 1,2,3,6\n" +
    //                    "10: 1,2,5,10\n" +
    //                    "15: 1,3,5,15\n" +
    //                    "21: 1,3,7,21\n" +
    //                    "28: 1,2,4,7,14,28\n" +
    //                    "We can see that 28 is the first triangle number to have over five divisors.\n" +
    //                    "What is the value of the first triangle number to have over five hundred divisors?",
    //                    220974, 5, Sol_12)
    //                },

    //                {
    //                    13,
    //                    new Problem("Large sum",
    //                    "Work out the first ten digits of the sum of the following one-hundred 50-digit numbers.\n" +
    //                    "37107287533902102798797998220837590246510135740250\n"+
    //                    "46376937677490009712648124896970078050417018260538\n"+
    //                    "74324986199524741059474233309513058123726617309629\n"+
    //                    "91942213363574161572522430563301811072406154908250\n"+
    //                    "23067588207539346171171980310421047513778063246676\n"+
    //                    "89261670696623633820136378418383684178734361726757\n"+
    //                    "28112879812849979408065481931592621691275889832738\n"+
    //                    "44274228917432520321923589422876796487670272189318\n"+
    //                    "47451445736001306439091167216856844588711603153276\n"+
    //                    "70386486105843025439939619828917593665686757934951\n"+
    //                    "62176457141856560629502157223196586755079324193331\n"+
    //                    "64906352462741904929101432445813822663347944758178\n"+
    //                    "92575867718337217661963751590579239728245598838407\n"+
    //                    "58203565325359399008402633568948830189458628227828\n"+
    //                    "80181199384826282014278194139940567587151170094390\n"+
    //                    "35398664372827112653829987240784473053190104293586\n"+
    //                    "86515506006295864861532075273371959191420517255829\n"+
    //                    "71693888707715466499115593487603532921714970056938\n"+
    //                    "54370070576826684624621495650076471787294438377604\n"+
    //                    "53282654108756828443191190634694037855217779295145\n"+
    //                    "36123272525000296071075082563815656710885258350721\n"+
    //                    "45876576172410976447339110607218265236877223636045\n"+
    //                    "17423706905851860660448207621209813287860733969412\n"+
    //                    "81142660418086830619328460811191061556940512689692\n"+
    //                    "51934325451728388641918047049293215058642563049483\n"+
    //                    "62467221648435076201727918039944693004732956340691\n"+
    //                    "15732444386908125794514089057706229429197107928209\n"+
    //                    "55037687525678773091862540744969844508330393682126\n"+
    //                    "18336384825330154686196124348767681297534375946515\n"+
    //                    "80386287592878490201521685554828717201219257766954\n"+
    //                    "78182833757993103614740356856449095527097864797581\n"+
    //                    "16726320100436897842553539920931837441497806860984\n"+
    //                    "48403098129077791799088218795327364475675590848030\n"+
    //                    "87086987551392711854517078544161852424320693150332\n"+
    //                    "59959406895756536782107074926966537676326235447210\n"+
    //                    "69793950679652694742597709739166693763042633987085\n"+
    //                    "41052684708299085211399427365734116182760315001271\n"+
    //                    "65378607361501080857009149939512557028198746004375\n"+
    //                    "35829035317434717326932123578154982629742552737307\n"+
    //                    "94953759765105305946966067683156574377167401875275\n"+
    //                    "88902802571733229619176668713819931811048770190271\n"+
    //                    "25267680276078003013678680992525463401061632866526\n"+
    //                    "36270218540497705585629946580636237993140746255962\n"+
    //                    "24074486908231174977792365466257246923322810917141\n"+
    //                    "91430288197103288597806669760892938638285025333403\n"+
    //                    "34413065578016127815921815005561868836468420090470\n"+
    //                    "23053081172816430487623791969842487255036638784583\n"+
    //                    "11487696932154902810424020138335124462181441773470\n"+
    //                    "63783299490636259666498587618221225225512486764533\n"+
    //                    "67720186971698544312419572409913959008952310058822\n"+
    //                    "95548255300263520781532296796249481641953868218774\n"+
    //                    "76085327132285723110424803456124867697064507995236\n"+
    //                    "37774242535411291684276865538926205024910326572967\n"+
    //                    "23701913275725675285653248258265463092207058596522\n"+
    //                    "29798860272258331913126375147341994889534765745501\n"+
    //                    "18495701454879288984856827726077713721403798879715\n"+
    //                    "38298203783031473527721580348144513491373226651381\n"+
    //                    "34829543829199918180278916522431027392251122869539\n"+
    //                    "40957953066405232632538044100059654939159879593635\n"+
    //                    "29746152185502371307642255121183693803580388584903\n"+
    //                    "41698116222072977186158236678424689157993532961922\n"+
    //                    "62467957194401269043877107275048102390895523597457\n"+
    //                    "23189706772547915061505504953922979530901129967519\n"+
    //                    "86188088225875314529584099251203829009407770775672\n"+
    //                    "11306739708304724483816533873502340845647058077308\n"+
    //                    "82959174767140363198008187129011875491310547126581\n"+
    //                    "97623331044818386269515456334926366572897563400500\n"+
    //                    "42846280183517070527831839425882145521227251250327\n"+
    //                    "55121603546981200581762165212827652751691296897789\n"+
    //                    "32238195734329339946437501907836945765883352399886\n"+
    //                    "75506164965184775180738168837861091527357929701337\n"+
    //                    "62177842752192623401942399639168044983993173312731\n"+
    //                    "32924185707147349566916674687634660915035914677504\n"+
    //                    "99518671430235219628894890102423325116913619626622\n"+
    //                    "73267460800591547471830798392868535206946944540724\n"+
    //                    "76841822524674417161514036427982273348055556214818\n"+
    //                    "97142617910342598647204516893989422179826088076852\n"+
    //                    "87783646182799346313767754307809363333018982642090\n"+
    //                    "10848802521674670883215120185883543223812876952786\n"+
    //                    "71329612474782464538636993009049310363619763878039\n"+
    //                    "62184073572399794223406235393808339651327408011116\n"+
    //                    "66627891981488087797941876876144230030984490851411\n"+
    //                    "60661826293682836764744779239180335110989069790714\n"+
    //                    "85786944089552990653640447425576083659976645795096\n"+
    //                    "66024396409905389607120198219976047599490197230297\n"+
    //                    "64913982680032973156037120041377903785566085089252\n"+
    //                    "16730939319872750275468906903707539413042652315011\n"+
    //                    "94809377245048795150954100921645863754710598436791\n"+
    //                    "78639167021187492431995700641917969777599028300699\n"+
    //                    "15368713711936614952811305876380278410754449733078\n"+
    //                    "40789923115535562561142322423255033685442488917353\n"+
    //                    "44889911501440648020369068063960672322193204149535\n"+
    //                    "41503128880339536053299340368006977710650566631954\n"+
    //                    "81234880673210146739058568557934581403627822703280\n"+
    //                    "82616570773948327592232845941706525094512325230608\n"+
    //                    "22918802058777319719839450180888072429661980811197\n"+
    //                    "77158542502016545090413245809786882778948721859617\n"+
    //                    "72107838435069186155435662884062257473692284509516\n"+
    //                    "20849603980134001723930671666823555245252804609722\n"+
    //                    "53503534226472524250874054075591789781264330331690",
    //                    84093, 5, Sol_13)
    //                },

    //                {
    //                    14,
    //                    new Problem("Longest Collatz sequence",
    //                    "The following iterative sequence is defined for the set of positive integers:\n" +
    //                    "n → n/2 (n is even)\n" +
    //                    "n → 3n + 1 (n is odd)\n" +
    //                    "Using the rule above and starting with 13, we generate the following sequence:\n" +
    //                    "13 → 40 → 20 → 10 → 5 → 16 → 8 → 4 → 2 → 1\n" +
    //                    "It can be seen that this sequence (starting at 13 and finishing at 1) contains 10 terms. Although it has not been proved yet (Collatz Problem), it is thought that all starting numbers finish at 1.\n" +
    //                    "Which starting number, under one million, produces the longest chain?",
    //                    226682, 5, Sol_14)
    //                },

    //                {
    //                    15,
    //                    new Problem("Lattice paths",
    //                    "Starting in the top left corner of a 2×2 grid, and only being able to move to the right and down, there are exactly 6 routes to the bottom right corner.\n" +
    //                    "How many such routes are there through a 20×20 grid?",
    //                    186737, 5, Sol_15)
    //                },

    //                {
    //                    16,
    //                    new Problem("Power digit sum",
    //                    "215 = 32768 and the sum of its digits is 3 + 2 + 7 + 6 + 8 = 26.\n" +
    //                    "What is the sum of the digits of the number 21000?",
    //                    229085, 5, Sol_16)
    //                },

    //                {
    //                    17,
    //                    new Problem("Number letter counts",
    //                    "If the numbers 1 to 5 are written out in words: one, two, three, four, five, then there are 3 + 3 + 5 + 4 + 4 = 19 letters used in total.\n" +
    //                    "If all the numbers from 1 to 1000 (one thousand) inclusive were written out in words, how many letters would be used?\n" +
    //                    "NOTE: Do not count spaces or hyphens. For example, 342 (three hundred and forty-two) contains 23 letters and 115 (one hundred and fifteen) contains 20 letters. The use of \"and\" when writing out numbers is in compliance with British usage.",
    //                    151178, 5, Sol_17)
    //                },

    //                {
    //                    18,
    //                    new Problem("Maximum path sum I",
    //                    "By starting at the top of the triangle below and moving to adjacent numbers on the row below, the maximum total from top to bottom is 23.\n" +
    //                    "TODO: Add grid\n" +
    //                    "That is, 3 + 7 + 4 + 9 = 23.\n" +
    //                    "Find the maximum total from top to bottom of the triangle below:\n" +
    //                    "TODO: Add grid\n" +
    //                    "NOTE: As there are only 16384 routes, it is possible to solve this problem by trying every route. However, Problem 67, is the same challenge with a triangle containing one-hundred rows; it cannot be solved by brute force, and requires a clever method! ;o)",
    //                    144606, 5, Sol_18)
    //                },

    //                {
    //                    19,
    //                    new Problem("Counting Sundays",
    //                    "You are given the following information, but you may prefer to do some research for yourself.\n" +
    //                    "How many Sundays fell on the first of the month during the twentieth century (1 Jan 1901 to 31 Dec 2000)?",
    //                    134530, 5, Sol_19)
    //                },

    //                {
    //                    31,
    //                    new Problem("Coin sums",
    //                    "In the United Kingdom the currency is made up of pound (£) and pence (p). There are eight coins in general circulation:\n" +
    //                    "1p, 2p, 5p, 10p, 20p, 50p, £1 (100p), and £2 (200p).\n" +
    //                    "It is possible to make £2 in the following way:\n" +
    //                    "1×£1 + 1×50p + 2×20p + 1×5p + 1×2p + 3×1p\n" +
    //                    "How many different ways can £2 be made using any number of coins?",
    //                    84093, 5, Sol_31)
    //                },
    //            };
    //    }

    //    public static string[] Sol_1() // Multiples of 3 and 5
    //    {
    //        // For every natural number under 1000
    //        int stopAt = 1000;
    //        int total = 0;
    //        for (int i = 1; i < stopAt; i++)
    //        {
    //            // Add it to the total if it is divisible by 3 or 5
    //            if (i % 3 == 0 || i % 5 == 0)
    //            {
    //                total += i;
    //            }
    //        }
    //        return new string[] { $"{total}", "" };
    //    }
    //    public static string[] Sol_2() // Even Fibbonachi numbers
    //    {
    //        List<int> Fib = new List<int>() { 1, 2 };
    //        int target = 4000000;
    //        long total = 2;
    //        while (Fib[Fib.Count - 1] < target) // While < 4000000
    //        {
    //            int nextFib = Fib[Fib.Count - 1] + Fib[Fib.Count - 2];
    //            Fib.Add(nextFib);
    //            if (nextFib % 2 == 0) // If even
    //            {
    //                total += nextFib;
    //            }
    //        }
    //        return new string[] { $"{total}", "" };
    //    }
    //    public static string[] Sol_3() //Largest prime factor
    //    {
    //        long numToFactor = 600851475143;

    //        // Get factors
    //        Dictionary<int, int> factors = Prime.GetPrimeFactors(numToFactor);
    //        return new string[] { $"{factors.Max()}", "" };
    //    }
    //    public static string[] Sol_4() //Largest palindrome product
    //    {
    //        //Largest palindrome product

    //        // I think it's best to check palindrome from largest to smallest.
    //        // Since it must be a multiple of two three-digit numbers, 998001 is going to be the starting point.
    //        // Instead of brute-forcing all multiplications (999*999, 998*999 ...), Lets go down from 998001 
    //        // and keep checking palindromes untill we find one that can be made with two three-digit numbers. 

    //        // Settings
    //        int Multdigits = 3;
    //        int Multamount = 2; // Doesn't work for inputs > 2 unfortunately.

    //        // Some preparatory calculations.
    //        int maxMult = Int32.Parse(String.Concat(Enumerable.Repeat("9", Multdigits))); // 999
    //        int maxTotal = Convert.ToInt32(Math.Pow(maxMult, Multamount)); // 999*999 = 998001

    //        int minMult = Convert.ToInt32(Math.Pow(10, Multdigits - 1)); // 100
    //        int minTotal = Convert.ToInt32(Math.Pow(minMult, Multamount)); // 100*100 = 100000

    //        string maxTotal_str = Math.Pow(maxMult, Multamount).ToString(); // 998001

    //        // Determine first palindrome
    //        int palindrome = Functions.Misc.GetLargestPalinDromeBelow(maxTotal);

    //        while (palindrome >= minTotal)
    //        {
    //            //Console.WriteLine(palindrome);

    //            // Now that we have a palindrome, lets check whether it can be made using two three-digit numbers
    //            for (int i = maxMult; i >= minMult; i--) // 999, 998, ... , 100
    //            {
    //                if (palindrome % i == 0) // If wholly divisible (to completely generalize: make this recursive)
    //                {
    //                    if ((palindrome / i).ToString().Length == Multdigits) // If the division creates a 3-digit number.
    //                    {
    //                        //Console.WriteLine(String.Format("Palindrome {0} can be made using {1} x {2}",palindrome,i,palindrome / i));
    //                        return new string[] { $"{palindrome}", "" }; // Return the succesfull palindrome.
    //                    }
    //                }
    //            }
    //            palindrome = Functions.Misc.GetLargestPalinDromeBelow(palindrome - 1);
    //        }
    //        // No solution was found.
    //        return new string[] { "No solution was found.", "I guess something went wrong..." };
    //    }
    //    public static string[] Sol_5() // Smallest multiple
    //    {
    //        List<int> ns = new List<int>();
    //        for (int i = 1; i <= 20; i++)
    //        {
    //            ns.Add(i);
    //        }
    //        return new string[] { $"{Functions.Misc.LowestCommonMultiple(ns)}", "" };
    //    }
    //    public static string[] Sol_6() // Sum square difference
    //    {


    //        int upTo = 100;
    //        double sumOfQuares = 0;
    //        int sum = 0;
    //        for (int i = 1; i <= upTo; i++)
    //        {
    //            sumOfQuares += Math.Pow(i, 2);
    //            sum += i;
    //        }
    //        double squareOfSum = Math.Pow(sum, 2);
    //        return new string[] { $"{Math.Abs(squareOfSum - sumOfQuares)}", $"Square of sums: {squareOfSum}\nSum of squares: {sumOfQuares}" };
    //    }
    //    public static string[] Sol_7() // 10001st prime
    //    {
    //        return new string[] { $"{Prime.GetPrimeAtIndex(10000)}", "" }; // See also the static class 'Prime'
    //    }
    //    public static string[] Sol_8()
    //    {
    //        int lengthOfProduct = 13;

    //        string N = "73167176531330624919225119674426574742355349194934" +
    //                   "96983520312774506326239578318016984801869478851843" +
    //                   "85861560789112949495459501737958331952853208805511" +
    //                   "12540698747158523863050715693290963295227443043557" +
    //                   "66896648950445244523161731856403098711121722383113" +
    //                   "62229893423380308135336276614282806444486645238749" +
    //                   "30358907296290491560440772390713810515859307960866" +
    //                   "70172427121883998797908792274921901699720888093776" +
    //                   "65727333001053367881220235421809751254540594752243" +
    //                   "52584907711670556013604839586446706324415722155397" +
    //                   "53697817977846174064955149290862569321978468622482" +
    //                   "83972241375657056057490261407972968652414535100474" +
    //                   "82166370484403199890008895243450658541227588666881" +
    //                   "16427171479924442928230863465674813919123162824586" +
    //                   "17866458359124566529476545682848912883142607690042" +
    //                   "24219022671055626321111109370544217506941658960408" +
    //                   "07198403850962455444362981230987879927244284909188" +
    //                   "84580156166097919133875499200524063689912560717606" +
    //                   "05886116467109405077541002256983155200055935729725" +
    //                   "71636269561882670428252483600823257530420752963450";

    //        // Finding the biggest product also means finding the biggest sum.
    //        // First we put them all into a big list
    //        List<string> sequences = new List<string>();

    //        for (int firstDigitIndex = 0; firstDigitIndex + lengthOfProduct <= N.Length; firstDigitIndex++)
    //        {
    //            // With products, order doesn't matter, so we sort the sequences from lowest to highest.
    //            string subStr = N.Substring(firstDigitIndex, lengthOfProduct);
    //            string sortedStr = String.Concat(subStr.ToCharArray().OrderBy(c => c.ToInt()));
    //            sequences.Add(sortedStr);
    //            //Console.WriteLine("Added sequence: " + sequences[^1]);
    //        }

    //        // Now that we have this list, lets go through caracter by caracter
    //        for (int i = 0; i < lengthOfProduct; i++)
    //        {
    //            Console.WriteLine(i);
    //            int highest = 0;

    //            // Find the sequences with the higest value at this index position and reduce the list to them.
    //            foreach (string seq in sequences)
    //            {
    //                //Console.WriteLine(seq);
    //                if (seq[i].ToInt() > highest)
    //                {
    //                    highest = seq[i].ToInt();
    //                    sequences = new List<string>() { seq };
    //                }
    //                else if (seq[i].ToInt() == highest)
    //                {
    //                    sequences.Add(seq);
    //                }
    //            }
    //        }

    //        long highestTotal = 0;
    //        foreach (string seq in sequences)
    //        {
    //            long subTotal = 1;
    //            foreach (char c in seq)
    //            {
    //                subTotal *= c.ToInt();
    //            }
    //            if (subTotal > highestTotal)
    //            {
    //                highestTotal = subTotal;
    //            }
    //        }
    //        return new string[] { $"{highestTotal}", $"Found at: { 0 }" };
    //    }

    //    public static string[] Sol_9()
    //    {
    //        // Special Pythagorean triplet

    //        // a+b+c=1000
    //        // a<b<c

    //        int[] triplet = new int[3];
    //        for (int k = 1; true; k++)
    //        {
    //            for (int m = 2; true; m++)
    //            {
    //                for (int n = 1; n < m; n++)
    //                {
    //                    triplet = Functions.Misc.GetPythagorianTriplet(m, n, k);
    //                    if (triplet.Sum() == 1000)
    //                    {
    //                        return new string[] { $"{triplet[0] * triplet[1] * triplet[2]}", $"({triplet[0]} * {triplet[1]} * {triplet[2]})" };
    //                    }
    //                }
    //                if (triplet.Sum() > 1000)
    //                {
    //                    break; // Start trying multiples (k>1)
    //                }
    //            }
    //        }
    //        //break;
    //    }

    //    public static string[] Sol_10()
    //    {
    //        // Find the sum of all the primes below two million.

    //        int limit = 2000000;

    //        // Calc primes
    //        Prime.CalcPrimes_UpToAndInc_N(limit - 1);

    //        // Calc sum
    //        long sum = 0;
    //        foreach (long P in Prime.Primes)
    //        {
    //            if (P >= limit)
    //            {
    //                break;
    //            }

    //            sum += P;
    //        }
    //        // Return
    //        return new string[] { $"{sum}", "" };
    //    }

    //    public static string[] Sol_11()
    //    {
    //        // Largest product in a grid

    //        int nInProd = 4;

    //        int[,] Grid = new int[,]{
    //            { 08, 02, 22, 97, 38, 15, 00, 40, 00, 75, 04, 05, 07, 78, 52, 12, 50, 77, 91, 08 },
    //            { 49, 49, 99, 40, 17, 81, 18, 57, 60, 87, 17, 40, 98, 43, 69, 48, 04, 56, 62, 00 },
    //            { 81, 49, 31, 73, 55, 79, 14, 29, 93, 71, 40, 67, 53, 88, 30, 03, 49, 13, 36, 65 },
    //            { 52, 70, 95, 23, 04, 60, 11, 42, 69, 24, 68, 56, 01, 32, 56, 71, 37, 02, 36, 91 },
    //            { 22, 31, 16, 71, 51, 67, 63, 89, 41, 92, 36, 54, 22, 40, 40, 28, 66, 33, 13, 80 },
    //            { 24, 47, 32, 60, 99, 03, 45, 02, 44, 75, 33, 53, 78, 36, 84, 20, 35, 17, 12, 50 },
    //            { 32, 98, 81, 28, 64, 23, 67, 10, 26, 38, 40, 67, 59, 54, 70, 66, 18, 38, 64, 70 },
    //            { 67, 26, 20, 68, 02, 62, 12, 20, 95, 63, 94, 39, 63, 08, 40, 91, 66, 49, 94, 21 },
    //            { 24, 55, 58, 05, 66, 73, 99, 26, 97, 17, 78, 78, 96, 83, 14, 88, 34, 89, 63, 72 },
    //            { 21, 36, 23, 09, 75, 00, 76, 44, 20, 45, 35, 14, 00, 61, 33, 97, 34, 31, 33, 95 },
    //            { 78, 17, 53, 28, 22, 75, 31, 67, 15, 94, 03, 80, 04, 62, 16, 14, 09, 53, 56, 92 }, // Why yes, I did do this all by hand.
    //            { 16, 39, 05, 42, 96, 35, 31, 47, 55, 58, 88, 24, 00, 17, 54, 24, 36, 29, 85, 57 },
    //            { 86, 56, 00, 48, 35, 71, 89, 07, 05, 44, 44, 37, 44, 60, 21, 58, 51, 54, 17, 58 },
    //            { 19, 80, 81, 68, 05, 94, 47, 69, 28, 73, 92, 13, 86, 52, 17, 77, 04, 89, 55, 40 },
    //            { 04, 52, 08, 83, 97, 35, 99, 16, 07, 97, 57, 32, 16, 26, 26, 79, 33, 27, 98, 66 },
    //            { 88, 36, 68, 87, 57, 62, 20, 72, 03, 46, 33, 67, 46, 55, 12, 32, 63, 93, 53, 69 },
    //            { 04, 42, 16, 73, 38, 25, 39, 11, 24, 94, 72, 18, 08, 46, 29, 32, 40, 62, 76, 36 },
    //            { 20, 69, 36, 41, 72, 30, 23, 88, 34, 62, 99, 69, 82, 67, 59, 85, 74, 04, 36, 16 },
    //            { 20, 73, 35, 29, 78, 31, 90, 01, 74, 31, 49, 71, 48, 86, 81, 16, 23, 57, 05, 54 },
    //            { 01, 70, 54, 71, 83, 51, 54, 69, 16, 92, 33, 48, 61, 43, 52, 01, 89, 19, 67, 48 }
    //            };

    //        int gridSize = Grid.GetLength(0); // Assuming its square
    //                                          //int[,] prodGrid = new int[gridSize, gridSize];

    //        int largestProductSoFar = 0;
    //        int[] largestProdInfo = new int[4];

    //        // For every row
    //        for (int y = 0; y < gridSize; y++)
    //        {
    //            // For every col
    //            for (int x = 0; x < gridSize; x++)
    //            {
    //                // Determine the checks that can be done at this coordinate
    //                List<Tuple<int, int>> dirSteps = new List<Tuple<int, int>>();

    //                // Horizontal (after x=3)
    //                if (x >= nInProd - 1)
    //                {
    //                    dirSteps.Add(new Tuple<int, int>(-1, 0));
    //                }

    //                // Vertical (after y=3)
    //                if (y >= nInProd - 1)
    //                {
    //                    dirSteps.Add(new Tuple<int, int>(0, -1));
    //                }

    //                // Diagonal TopLeft (after x,y = 3)
    //                if (y >= nInProd - 1 && x >= nInProd - 1)
    //                {
    //                    dirSteps.Add(new Tuple<int, int>(-1, -1));
    //                }

    //                // Diagonal BottomLeft (after x = 3 to x = gridSize-4)
    //                if (x >= nInProd - 1 && y <= gridSize - nInProd)
    //                {
    //                    dirSteps.Add(new Tuple<int, int>(-1, 1));
    //                }

    //                // For each direction
    //                foreach ((int dx, int dy) in dirSteps)
    //                {
    //                    int prod = 1;

    //                    // Calculate the product stepwise
    //                    for (int i = 0; i < nInProd; i++)
    //                    {
    //                        prod *= Grid[y + dy * i, x + dx * i];
    //                    }

    //                    // Check against largest so far
    //                    if (prod > largestProductSoFar)
    //                    {
    //                        largestProductSoFar = prod;
    //                        largestProdInfo = new int[] { x, y, dx, dy };
    //                    }
    //                }
    //            }
    //        }
    //        return new string[] { $"{largestProductSoFar}", $"Found at: ({largestProdInfo[0]},{largestProdInfo[1]}), dir: ({largestProdInfo[2]},{largestProdInfo[3]})" };
    //    }

    //    public static string[] Sol_12()
    //    {
    //        // What is the value of the first triangle number to have over five hundred divisors?

    //        int divAm_0;
    //        int divAm_1 = 1;

    //        int highestDivAm = 0;

    //        for (int n = 1; true; n++)
    //        {
    //            // The n'th triangle number is equal to (n(n+1))/2
    //            // Since the divisor function is multiplicative and n and n+1 are coprime,
    //            // the amount of divisors for triangle number tT_n d(t_n) is equal to d(t_(n/2)) * d(t_((n+1)/2))

    //            divAm_0 = divAm_1;

    //            if (n % 2 == 0)
    //            {
    //                divAm_0 = Functions.Misc.GetNumOfDivisors(n / 2);
    //                divAm_1 = Functions.Misc.GetNumOfDivisors(n + 1);
    //            }
    //            else
    //            {
    //                divAm_0 = divAm_1;
    //                divAm_1 = Functions.Misc.GetNumOfDivisors((n + 1) / 2);
    //            }

    //            int divAm = divAm_0 * divAm_1;

    //            if (divAm > highestDivAm)
    //            {
    //                highestDivAm = divAm;
    //                int t = (n * (n + 1)) / 2;
    //                //Console.WriteLine($"n: {n}, t: {t}, divAm: {divAm}");
    //            }


    //            // Check if amount reached
    //            if (divAm > 500)
    //            {
    //                int t = (n * (n + 1)) / 2;
    //                Functions.Misc.CalcDivisorsOfN(t);

    //                string s = $"Divisors: ({divAm})\n";

    //                foreach (int d in Functions.Misc.divisorsOfN[t])
    //                {
    //                    s += $"{d}, ";
    //                }

    //                s.Remove(s.Length - 2);

    //                return new string[] { $"{t}", $"{s}" };
    //            }
    //        }
    //    }

    //    public static string[] Sol_13()
    //    {
    //        // Large sum

    //        string data = "37107287533902102798797998220837590246510135740250\n" +
    //                        "46376937677490009712648124896970078050417018260538\n" +
    //                        "74324986199524741059474233309513058123726617309629\n" +
    //                        "91942213363574161572522430563301811072406154908250\n" +
    //                        "23067588207539346171171980310421047513778063246676\n" +
    //                        "89261670696623633820136378418383684178734361726757\n" +
    //                        "28112879812849979408065481931592621691275889832738\n" +
    //                        "44274228917432520321923589422876796487670272189318\n" +
    //                        "47451445736001306439091167216856844588711603153276\n" +
    //                        "70386486105843025439939619828917593665686757934951\n" +
    //                        "62176457141856560629502157223196586755079324193331\n" +
    //                        "64906352462741904929101432445813822663347944758178\n" +
    //                        "92575867718337217661963751590579239728245598838407\n" +
    //                        "58203565325359399008402633568948830189458628227828\n" +
    //                        "80181199384826282014278194139940567587151170094390\n" +
    //                        "35398664372827112653829987240784473053190104293586\n" +
    //                        "86515506006295864861532075273371959191420517255829\n" +
    //                        "71693888707715466499115593487603532921714970056938\n" +
    //                        "54370070576826684624621495650076471787294438377604\n" +
    //                        "53282654108756828443191190634694037855217779295145\n" +
    //                        "36123272525000296071075082563815656710885258350721\n" +
    //                        "45876576172410976447339110607218265236877223636045\n" +
    //                        "17423706905851860660448207621209813287860733969412\n" +
    //                        "81142660418086830619328460811191061556940512689692\n" +
    //                        "51934325451728388641918047049293215058642563049483\n" +
    //                        "62467221648435076201727918039944693004732956340691\n" +
    //                        "15732444386908125794514089057706229429197107928209\n" +
    //                        "55037687525678773091862540744969844508330393682126\n" +
    //                        "18336384825330154686196124348767681297534375946515\n" +
    //                        "80386287592878490201521685554828717201219257766954\n" +
    //                        "78182833757993103614740356856449095527097864797581\n" +
    //                        "16726320100436897842553539920931837441497806860984\n" +
    //                        "48403098129077791799088218795327364475675590848030\n" +
    //                        "87086987551392711854517078544161852424320693150332\n" +
    //                        "59959406895756536782107074926966537676326235447210\n" +
    //                        "69793950679652694742597709739166693763042633987085\n" +
    //                        "41052684708299085211399427365734116182760315001271\n" +
    //                        "65378607361501080857009149939512557028198746004375\n" +
    //                        "35829035317434717326932123578154982629742552737307\n" +
    //                        "94953759765105305946966067683156574377167401875275\n" +
    //                        "88902802571733229619176668713819931811048770190271\n" +
    //                        "25267680276078003013678680992525463401061632866526\n" +
    //                        "36270218540497705585629946580636237993140746255962\n" +
    //                        "24074486908231174977792365466257246923322810917141\n" +
    //                        "91430288197103288597806669760892938638285025333403\n" +
    //                        "34413065578016127815921815005561868836468420090470\n" +
    //                        "23053081172816430487623791969842487255036638784583\n" +
    //                        "11487696932154902810424020138335124462181441773470\n" +
    //                        "63783299490636259666498587618221225225512486764533\n" +
    //                        "67720186971698544312419572409913959008952310058822\n" +
    //                        "95548255300263520781532296796249481641953868218774\n" +
    //                        "76085327132285723110424803456124867697064507995236\n" +
    //                        "37774242535411291684276865538926205024910326572967\n" +
    //                        "23701913275725675285653248258265463092207058596522\n" +
    //                        "29798860272258331913126375147341994889534765745501\n" +
    //                        "18495701454879288984856827726077713721403798879715\n" +
    //                        "38298203783031473527721580348144513491373226651381\n" +
    //                        "34829543829199918180278916522431027392251122869539\n" +
    //                        "40957953066405232632538044100059654939159879593635\n" +
    //                        "29746152185502371307642255121183693803580388584903\n" +
    //                        "41698116222072977186158236678424689157993532961922\n" +
    //                        "62467957194401269043877107275048102390895523597457\n" +
    //                        "23189706772547915061505504953922979530901129967519\n" +
    //                        "86188088225875314529584099251203829009407770775672\n" +
    //                        "11306739708304724483816533873502340845647058077308\n" +
    //                        "82959174767140363198008187129011875491310547126581\n" +
    //                        "97623331044818386269515456334926366572897563400500\n" +
    //                        "42846280183517070527831839425882145521227251250327\n" +
    //                        "55121603546981200581762165212827652751691296897789\n" +
    //                        "32238195734329339946437501907836945765883352399886\n" +
    //                        "75506164965184775180738168837861091527357929701337\n" +
    //                        "62177842752192623401942399639168044983993173312731\n" +
    //                        "32924185707147349566916674687634660915035914677504\n" +
    //                        "99518671430235219628894890102423325116913619626622\n" +
    //                        "73267460800591547471830798392868535206946944540724\n" +
    //                        "76841822524674417161514036427982273348055556214818\n" +
    //                        "97142617910342598647204516893989422179826088076852\n" +
    //                        "87783646182799346313767754307809363333018982642090\n" +
    //                        "10848802521674670883215120185883543223812876952786\n" +
    //                        "71329612474782464538636993009049310363619763878039\n" +
    //                        "62184073572399794223406235393808339651327408011116\n" +
    //                        "66627891981488087797941876876144230030984490851411\n" +
    //                        "60661826293682836764744779239180335110989069790714\n" +
    //                        "85786944089552990653640447425576083659976645795096\n" +
    //                        "66024396409905389607120198219976047599490197230297\n" +
    //                        "64913982680032973156037120041377903785566085089252\n" +
    //                        "16730939319872750275468906903707539413042652315011\n" +
    //                        "94809377245048795150954100921645863754710598436791\n" +
    //                        "78639167021187492431995700641917969777599028300699\n" +
    //                        "15368713711936614952811305876380278410754449733078\n" +
    //                        "40789923115535562561142322423255033685442488917353\n" +
    //                        "44889911501440648020369068063960672322193204149535\n" +
    //                        "41503128880339536053299340368006977710650566631954\n" +
    //                        "81234880673210146739058568557934581403627822703280\n" +
    //                        "82616570773948327592232845941706525094512325230608\n" +
    //                        "22918802058777319719839450180888072429661980811197\n" +
    //                        "77158542502016545090413245809786882778948721859617\n" +
    //                        "72107838435069186155435662884062257473692284509516\n" +
    //                        "20849603980134001723930671666823555245252804609722\n" +
    //                        "53503534226472524250874054075591789781264330331690";

    //        string[] dataArr = data.Split('\n');
    //        int numLen = 50;
    //        int ansLen = 10;

    //        int[] subSums = new int[numLen];
    //        long sum = 0;


    //        // For every digit
    //        for (int dI = 0; dI < numLen; dI++)
    //        {
    //            // Increment sum
    //            sum *= 10;

    //            // For every number
    //            foreach (string number in dataArr)
    //            {
    //                // Add this char to sum
    //                sum += number[dI] - '0';
    //            }

    //            // If sum has reached desired length
    //            if (sum.ToString().Length >= ansLen)
    //            {
    //                // Check if adding 450 (9*50) would change the first 10 chars of sum
    //                if ((sum + 9 * numLen).ToString().StartsWith(sum.ToString().Substring(0, ansLen)))
    //                {
    //                    break;
    //                }
    //            }
    //        }
    //        return new string[] { $"{sum.ToString().Substring(0, ansLen)}", "" };
    //    }

    //    public static string[] Sol_14()
    //    {
    //        //Longest Collatz sequence

    //        // n → n / 2(n is even)
    //        // n → 3n + 1(n is odd)

    //        int upperLimit = 1000000;

    //        // Big array will store the length of the sequence for n to go to 1 at index n.
    //        int[] seqLengthArray = new int[upperLimit];

    //        seqLengthArray[1] = 1;

    //        // We loop over every number under 1000000 and run through the sequence
    //        // unless we come across a number for which the length of the sequence
    //        // is allready known. 0 and 1 will have a seqLength of 0.
    //        for (int i = 2; i < upperLimit; i++)
    //        {
    //            long n = i;
    //            while (true)
    //            {
    //                // If the length of the sequence is known from here (initialized to 0), we add it to the current and break.
    //                if (n < upperLimit && seqLengthArray[n] != 0)
    //                {
    //                    seqLengthArray[i] += seqLengthArray[n];
    //                    break;
    //                }
    //                // The collatz choice
    //                if (n % 2 == 0)
    //                {
    //                    n /= 2;
    //                }
    //                else
    //                {
    //                    n *= 3;
    //                    n += 1;
    //                }
    //                // Add 1 step
    //                seqLengthArray[i] += 1;
    //            }
    //        }
    //        int max = seqLengthArray.Max();
    //        int maxI = Array.IndexOf(seqLengthArray, max);
    //        return new string[] { $"{maxI}", $"Found highest sequence length: { max }\nWith steps:\n{ String.Join("\n", Functions.Misc.getCollatz(maxI).Select(p => p.ToString()).ToArray()) }" };
    //    }

    //    public static Object[] Sol_15()
    //    {
    //        #region Solution

    //        SW.Reset();
    //        SW.Start();

    //        //Lattice paths
    //        int x = 20;

    //        long[,] pathAmount = new long[x + 1, x + 1];

    //        for (int rI = 0; rI < x + 1; rI++)
    //        {

    //            for (int cI = rI; cI < x + 1; cI++)
    //            {
    //                if (rI == 0)
    //                {
    //                    pathAmount[rI, cI] = 1;
    //                }
    //                else if (rI == cI)
    //                {
    //                    pathAmount[rI, cI] = 2 * pathAmount[rI - 1, cI];
    //                }
    //                else
    //                {
    //                    pathAmount[rI, cI] = pathAmount[rI - 1, cI] + pathAmount[rI, cI - 1];
    //                }
    //            }
    //        }
    //        long solution = pathAmount[x, x];
    //        SW.Stop();

    //        #endregion

    //        // Return solution, solutionTime, solutionDiscription
    //        return new Object[] { $"{solution}", SW.ElapsedMilliseconds};
    //    }

    //    public static Object[] Sol_16()
    //    {
    //        // What is the sum of the digits of the number 2^1000?

    //        #region Solution

    //        int power = 1000;
    //        long solution = 0;

    //        SW.Reset();
    //        SW.Start();

    //        int[] digits = new int[(int)(1 + power * Math.Log10(2))];
    //        digits[digits.Length - 1] = 1;

    //        for (int i = 0; i < power; i++)
    //        {
    //            // Multiply each digit by 2
    //            for (int dI = 0; dI < digits.Length; dI++)
    //            {
    //                digits[dI] *= 2;
    //            }
    //            // If any digit is >9 ==> carry over the 1.
    //            for (int dI = digits.Length - 1; dI >= 0; dI--)
    //            {
    //                // Assuming <100
    //                if (digits[dI] > 9)
    //                {
    //                    digits[dI - 1] += digits[dI] / 10;
    //                    digits[dI] -= 10;
    //                }
    //            }
    //        }
    //        solution = digits.Sum();

    //        SW.Stop();

    //        // Return solution, solutionTime, solutionDiscription
    //        return new Object[] { $"{solution}", SW.ElapsedMilliseconds};
    //    }

    //    public static Object[] Sol_17()
    //    {
    //        // If all the numbers from 1 to 1000 (one thousand) inclusive were written out in words, how many letters would be used?

    //        #region Solution

    //        int limit = 1001;
    //        int solution = 0;

    //        SW.Reset();
    //        SW.Start();

    //        for (int i = 1; i < limit; i++)
    //        {
    //            solution += Functions.Misc.IntToVerboseString(i).RemoveAll(' ').RemoveAll('-').Length;
    //        }

    //        SW.Stop();

    //        #endregion

    //        // Return solution, solutionTime, solutionDiscription
    //        return new Object[] { $"{solution}", SW.ElapsedMilliseconds};
    //    }

    //    public static Object[] Sol_18()
    //    {
    //        #region Solution

    //        long solution = 0;

    //        // I don't value my time
    //        int[,] T = new int[,]
    //        {
    //                { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 75, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 95, 00, 64, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 17, 00, 47, 00, 82, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 18, 00, 35, 00, 87, 00, 10, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 20, 00, 04, 00, 82, 00, 47, 00, 65, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 00, 00, 00, 19, 00, 01, 00, 23, 00, 75, 00, 03, 00, 34, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 00, 00, 88, 00, 02, 00, 77, 00, 73, 00, 07, 00, 63, 00, 67, 00, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 00, 99, 00, 65, 00, 04, 00, 28, 00, 06, 00, 16, 00, 70, 00, 92, 00, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 00, 41, 00, 41, 00, 26, 00, 56, 00, 83, 00, 40, 00, 80, 00, 70, 00, 33, 00, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 00, 41, 00, 48, 00, 72, 00, 33, 00, 47, 00, 32, 00, 37, 00, 16, 00, 94, 00, 29, 00, 00, 00, 00, 00 },
    //                { 00, 00, 00, 00, 53, 00, 71, 00, 44, 00, 65, 00, 25, 00, 43, 00, 91, 00, 52, 00, 97, 00, 51, 00, 14, 00, 00, 00, 00 },
    //                { 00, 00, 00, 70, 00, 11, 00, 33, 00, 28, 00, 77, 00, 73, 00, 17, 00, 78, 00, 39, 00, 68, 00, 17, 00, 57, 00, 00, 00 },
    //                { 00, 00, 91, 00, 71, 00, 52, 00, 38, 00, 17, 00, 14, 00, 91, 00, 43, 00, 58, 00, 50, 00, 27, 00, 29, 00, 48, 00, 00 },
    //                { 00, 63, 00, 66, 00, 04, 00, 68, 00, 89, 00, 53, 00, 67, 00, 30, 00, 73, 00, 16, 00, 69, 00, 87, 00, 40, 00, 31, 00 },
    //                { 04, 00, 62, 00, 98, 00, 27, 00, 23, 00, 09, 00, 70, 00, 98, 00, 73, 00, 93, 00, 38, 00, 53, 00, 60, 00, 04, 00, 23 },
    //        };

    //        SW.Reset();
    //        SW.Start();

    //        // Start at the second row from the bottom and work our way upwards
    //        for (int rowI = T.GetLength(0) - 2; rowI >= 0; rowI--)
    //        {
    //            // For each number that is not 0
    //            for (int colI = 0; colI < T.GetLength(1); colI++)
    //            {
    //                if (T[rowI, colI] != 0)
    //                {
    //                    // Add the highest number below it to itself
    //                    T[rowI, colI] += Math.Max(T[rowI + 1, colI - 1], T[rowI + 1, colI + 1]);
    //                }
    //            }
    //        }
    //        solution = T[0, T.GetLength(1) / 2];

    //        SW.Stop();

    //        #endregion



    //        // Return solution, solutionTime, solutionDiscription
    //        return new Object[] { $"{solution}", SW.ElapsedMilliseconds};
    //    }

    //    public static Object[] Sol_19()
    //    {
    //        // How many Sundays fell on the first of the month during the twentieth century (1 Jan 1901 to 31 Dec 2000)?

    //        #region Solution

    //        long solution = 0;

    //        SW.Reset();
    //        SW.Start();

    //        for (DateTime date = new DateTime(1901, 1, 1); date.Date <= new DateTime(2000, 12, 31).Date; date = date.AddMonths(1))
    //        {
    //            if (date.DayOfWeek == DayOfWeek.Sunday)
    //            {
    //                solution++;
    //            }
    //        }

    //        SW.Stop();

    //        #endregion

    //        // Return solution, solutionTime, solutionDiscription
    //        return new Object[] { $"{solution}", SW.ElapsedMilliseconds};
    //    }

    //    public static string[] Sol_31()
    //    {
    //        // Coin sums

    //        int[] coins = { 1, 2, 5, 10, 20, 50, 100, 200 };
    //        int target = 200;
    //        int[] waysToMake = new int[target + 1]; // 0,1,2,3...200

    //        waysToMake[0] = 1; // We can't make 0p using the coins, but this is done for handiness' sake.
    //                           // In the algorithm, this amounts to adding 1 possibility every time t-coin is 0p.

    //        foreach (int coin in coins) // For every coin
    //        {
    //            for (int t = coin; t <= target; t++)
    //            {
    //                // Using only coins <= coin, how many ways to make target?
    //                waysToMake[t] += waysToMake[t - coin];
    //            }
    //        }
    //        return new string[] { $"{waysToMake[200]}", "" };
    //    }

    //    public static Object[] Sol_TEMPLATE()
    //    {
    //        long solution = 0;

    //        SW.Reset();
    //        SW.Start();

    //        #region Solution

    //        #endregion Solution

    //        SW.Stop();

    //        // Return solution, solutionTime, solutionDiscription
    //        return new Object[] { $"{solution}", SW.ElapsedMilliseconds};
    //    }
    //}


}
