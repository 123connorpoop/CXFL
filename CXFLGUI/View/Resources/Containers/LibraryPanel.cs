﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CsXFL;
using MauiIcons.Core;
using MauiIcons.Material;
using Microsoft.Maui.Controls.Shapes;
using static MainViewModel;

namespace CXFLGUI
{
    public class LibraryPanel : VanillaFrame
    {
        private MainViewModel viewModel;
        private Label Label_LibraryCount;

        int LoadedItemsCount = 0;
        int LibraryRowHeight = 35;

        // Library Tabs
        double nameWidth = 300;
        double useCountWidth = 50;
        double dateModifiedWidth = 100;
        double MIN_WIDTH = 50;

        Dictionary<string, CsXFL.Item> Tuple_LibraryItemDict = new Dictionary<string, CsXFL.Item>();
        ObservableCollection<LibraryItem> LibraryItems = new ObservableCollection<LibraryItem>();

        public class LibraryItem
        {
            public string Key { get; set; }
            public CsXFL.Item Value { get; set; }
        }

        public static ImageButton CreateIconButton(MaterialIcons icon, int size, string tooltip = null, Style buttonStyle = null)
        {
            var button = new ImageButton().Icon(icon);
            button.HeightRequest = size;
            button.WidthRequest = size;

            if (!string.IsNullOrEmpty(tooltip))
            {
                ToolTipProperties.SetText(button, tooltip);
            }

            if (buttonStyle != null)
            {
                button.Style = buttonStyle;
            }

            return button;
        }

        private void DocumentOpened(object sender, DocumentEventArgs e)
        {
            Document Doc = e.Document;
            LoadedItemsCount = Doc.Library.Items.Count;
            UpdateLibraryCount(LoadedItemsCount);
            Tuple_LibraryItemDict = Doc.Library.Items;

            // Clear existing items
            LibraryItems.Clear();

            // Initiating conversion sequence!
            foreach (var kvp in Tuple_LibraryItemDict)
            {
                LibraryItems.Add(new LibraryItem { Key = kvp.Key, Value = kvp.Value });
            }
        }

        private void UpdateLibraryCount(int LoadedItemsCount)
        {
            Label_LibraryCount.Text = LoadedItemsCount + " Items";
        }

        private List<LibraryItem> SortLibraryItems(List<LibraryItem> items)
        {
            // Naive alphabetical sort, doesn't accomodate file structure
            return items.OrderBy(item => item.Key).ToList();
        }
        private MenuItem CreateMenuItem(string text, Action action)
        {
            return new MenuItem
            {
                Text = text,
                Command = new Command(action)
            };
        }
        Label CreateLabel(string text, double width)
        {
            return new Label
            {
                Text = text,
                TextColor = Colors.White,
                WidthRequest = width,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
            };
        }

        private void Library_CellSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem == null)
                return;

            // Do something with the selected item (e.SelectedItem)
            Trace.WriteLine("Selected Item", e.SelectedItem.ToString());

            // Deselect the item
            ((ListView)sender).SelectedItem = null;
        }

            public class DraggableSeparator : Grid
            {
                private readonly BoxView visualIndicator;
                private readonly BoxView hitArea;
                private double initialX;
                private bool isDragging;
                private View leftElement;
                private View rightElement;

                public DraggableSeparator(View leftElement, View rightElement)
                {
                    this.leftElement = leftElement;
                    this.rightElement = rightElement;

                    visualIndicator = new BoxView
                    {
                        BackgroundColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        WidthRequest = 0.5,
                    };

                    hitArea = new BoxView
                    {
                        BackgroundColor = Colors.Transparent,
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        WidthRequest = 5,
                    };

                    var gestureRecognizer = new PanGestureRecognizer();
                    gestureRecognizer.PanUpdated += OnPanUpdated;
                    hitArea.GestureRecognizers.Add(gestureRecognizer);

                    Children.Add(visualIndicator);
                    Children.Add(hitArea);
                }

                private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
                {
                    switch (e.StatusType)
                    {
                        case GestureStatus.Started:
                            initialX = e.TotalX;
                            isDragging = true;
                            break;
                        case GestureStatus.Running when isDragging:
                            double offset = e.TotalX - initialX;

                            if (leftElement != null)
                            {
                                leftElement.WidthRequest = Math.Max(leftElement.WidthRequest + offset, 0);
                            }

                            if (rightElement != null)
                            {
                                rightElement.WidthRequest = Math.Max(rightElement.WidthRequest - offset, 0);
                            }

                            initialX = e.TotalX;
                            break;
                        case GestureStatus.Canceled:
                        case GestureStatus.Completed:
                            isDragging = false;
                            break;
                    }
                }
            }

        public LibraryPanel(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
            viewModel.DocumentOpened += DocumentOpened;

            // Top / Bottom of Library Pane
            var StackLayout_Pane = new StackLayout()
            {
                Padding = new Thickness(0, 25, 0, 0)
            };

            // Library Count
            Label_LibraryCount = new Label()
            {
                TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"],
                Padding = new Thickness(10, 5, 20, 5),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
            };

            UpdateLibraryCount(0);

            // SearchBar
            SearchBar SearchBar_Library = new SearchBar
            {
                Style = (Style)App.Fixed_ResourceDictionary["DefaultSearchBar"]["Style"],
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Placeholder = "Search...",
            };

            var SearchPadding = new Microsoft.Maui.Controls.Frame
            {
                Padding = Padding = new Thickness(10, 5, 20, 5),
                HasShadow = true
            };

            // Horizontal Divider between Library Count and SearchBar
            Grid HzDivider = new Grid
            {
                ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
            };

            Grid.SetColumn(Label_LibraryCount, 0);
            Grid.SetRow(Label_LibraryCount, 0);
            HzDivider.Children.Add(Label_LibraryCount);

            Grid.SetColumn(SearchBar_Library, 1);
            Grid.SetRow(SearchBar_Library, 0);
            HzDivider.Children.Add(SearchBar_Library);

            // Library Display
            var SortedLibraryItems = SortLibraryItems(LibraryItems.ToList());
            var ListView_LibraryDisplay = new ListView
            {
                ItemsSource = LibraryItems,
                RowHeight = LibraryRowHeight
            };

            var nameLabel = CreateLabel("Name", nameWidth);
            var useCountLabel = CreateLabel("Use Count", useCountWidth);
            var dateModifiedLabel = CreateLabel("Date Modified", dateModifiedWidth);

            var LibraryDisplayTabs = new Border
            {
                Background = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryDark"],
                Stroke = (Color)App.Fixed_ResourceDictionary["Colors"]["White"],
                StrokeThickness = 0.5,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(25, 25, 0, 0)
                },
                HeightRequest = 25,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Content = new StackLayout
                {
                    Children =
                    {
                        nameLabel,
                        new DraggableSeparator(nameLabel, useCountLabel),
                        useCountLabel,
                        new DraggableSeparator(useCountLabel, dateModifiedLabel),
                        dateModifiedLabel,
                    },
                    Orientation = StackOrientation.Horizontal
                }
            };

            // Add the gesture recognizer to the parent container (ContentPage in this case)
            // Note, this is stupid.
            //LibraryDisplayTabs.GestureRecognizers.Add(separatorDragGesture);

            var Collection_LibraryDisplay = new CollectionView
            {
                EmptyView = new Grid
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    Children =
                        {
                            new Label
                            {
                                Text = "No library items.",
                                FontSize = 16,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            }
                        }
                },
                ItemsSource = LibraryItems,
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
                ItemTemplate = new DataTemplate(() =>
                {
                    var LibraryCell_Entry = new Entry()
                    {
                        Style = (Style)App.Fixed_ResourceDictionary["DefaultEntry"]["Style"],
                        TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"],
                        IsSpellCheckEnabled = false
                    };

                    LibraryCell_Entry.SetBinding(Entry.TextProperty, "Key");

                    var LibraryCell_Icon = new ImageButton
                    {
                        Style = (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Style"],
                    };

                    LibraryCell_Icon.Icon(MaterialIcons.QuestionMark);

                    //Visually unfocus when enter button is pressed
                    LibraryCell_Entry.Completed += (sender, e) =>
                    {
                        LibraryCell_Entry.Unfocus();
                    };

                    var Library_StackCell = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        VerticalOptions = LayoutOptions.Center,
                        Padding = new Thickness(0, 0),
                        Spacing = 10,
                        Children = { LibraryCell_Icon, LibraryCell_Entry }
                    };

                    // MVVM Stuff Here
                    
                    return Library_StackCell;

                })
            };

            var scrollView = new ScrollView
            {
                Content = Collection_LibraryDisplay,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            var stackLayout = new StackLayout
            {
                Children = { scrollView },
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            // Keep right now for reference

            //ListView_LibraryDisplay.ItemTemplate = new DataTemplate(() =>
            //{
            //    var Library_ViewCell = new ViewCell();

            //    var LibraryCell_Entry = new Entry()
            //    {
            //        Style = (Style)App.Fixed_ResourceDictionary["DefaultEntry"]["Style"],
            //        TextColor = (Color)App.Fixed_ResourceDictionary["Colors"]["PrimaryText"],
            //        IsSpellCheckEnabled = false
            //    };

            //    var LibraryCell_Icon = new ImageButton
            //    {
            //        Style = (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Style"],
            //    };

            //    // Visually unfocus when enter button is pressed
            //    LibraryCell_Entry.Completed += (sender, e) =>
            //    {
            //        LibraryCell_Entry.Unfocus();
            //    };

            //    Library_ViewCell.View = new StackLayout
            //    {
            //        Orientation = StackOrientation.Horizontal,
            //        VerticalOptions = LayoutOptions.Center,
            //        Padding = new Thickness(0, 0),
            //        Spacing = 10,
            //        Children = { LibraryCell_Icon, LibraryCell_Entry }
            //    };

            //    Library_ViewCell.BindingContextChanged += (sender, e) =>
            //    {
            //        if (Library_ViewCell.BindingContext is LibraryItem item)
            //        {
            //            // itemType agnostic context options
            //            Library_ViewCell.ContextActions.Add(CreateMenuItem("Cut", () =>
            //            {
            //                Trace.WriteLine("Cutting " + Library_ViewCell.ToString());
            //            }));

            //            Library_ViewCell.ContextActions.Add(CreateMenuItem("Copy", () =>
            //            {
            //                Trace.WriteLine("Copying " + Library_ViewCell.ToString());
            //            }));

            //            Library_ViewCell.ContextActions.Add(CreateMenuItem("Paste", () =>
            //            {
            //                Trace.WriteLine("Pasting " + Library_ViewCell.ToString());
            //            }));

            //            // Offset folder children
            //            int forwardslashCount = item.Key.Count(c => c == '/');
            //            ((StackLayout)Library_ViewCell.View).Margin = new Thickness(forwardslashCount * 25, 0, 0, 0);

            //            // Normalize the library name
            //            int lastForwardsSlash = item.Key.LastIndexOf('/');
            //            if (lastForwardsSlash >= 0 && lastForwardsSlash < item.Key.Length - 1)
            //            {
            //                LibraryCell_Entry.Text = item.Key.Substring(lastForwardsSlash + 1);
            //            }
            //            else
            //            {
            //                LibraryCell_Entry.Text = item.Key;
            //            }

            //            // itemType dependent icons and context options 
            //            switch (item.Value.ItemType)
            //            {
            //                case "bitmap":
            //                    LibraryCell_Icon.Icon(MaterialIcons.Image);
            //                    break;
            //                case "sound":
            //                    LibraryCell_Icon.Icon(MaterialIcons.VolumeUp);
            //                    Library_ViewCell.ContextActions.Add(CreateMenuItem("Play", () =>
            //                    {
            //                        Trace.WriteLine("Playing " + Library_ViewCell.ToString());
            //                    }));

            //                    break;
            //                case "graphic":
            //                    LibraryCell_Icon.Icon(MaterialIcons.Category);
            //                    Library_ViewCell.ContextActions.Add(CreateMenuItem("Export to SWF", () =>
            //                    {
            //                        Trace.WriteLine("Exporting to SWF " + Library_ViewCell.ToString());
            //                    }));
            //                    break;
            //                case "movie clip":
            //                    LibraryCell_Icon.Icon(MaterialIcons.Movie);
            //                    break;
            //                case "font":
            //                    LibraryCell_Icon.Icon(MaterialIcons.ABC);
            //                    break;
            //                case "button":
            //                    LibraryCell_Icon.Icon(MaterialIcons.SmartButton);
            //                    break;
            //                case "folder":
            //                    LibraryCell_Icon.Icon(MaterialIcons.Folder);
            //                    break;
            //                default:
            //                    LibraryCell_Icon.Icon(MaterialIcons.QuestionMark);
            //                    break;
            //            }
            //        }
            //    };

            //    return Library_ViewCell;
            //});

            //ListView_LibraryDisplay.ItemSelected += Library_CellSelected;

            // SearchBar logic
            SearchBar_Library.TextChanged += (sender, e) =>
            {
                string searchText = e.NewTextValue.ToLower();
                var filteredItems = LibraryItems.Where(item =>
                {
                    return item.Key.ToLower().Contains(searchText) || item.Value.ItemType.ToLower().Contains(searchText);
                }).ToList();

                ListView_LibraryDisplay.ItemsSource = filteredItems;
            };

            // Footer
            var footerGrid = new Grid
            {
                Padding = new Thickness(10, 5),
                HorizontalOptions = LayoutOptions.Start,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            int ButtonSize = 25;
            Style IconButtonStyle = (Style)App.Fixed_ResourceDictionary["DefaultImageButton"]["Style"];

            var NewSymbol = CreateIconButton(MaterialIcons.Add, ButtonSize, "New Symbol", IconButtonStyle);
            var NewFolder = CreateIconButton(MaterialIcons.Folder, ButtonSize, "New Folder", IconButtonStyle);
            var EditProperties = CreateIconButton(MaterialIcons.Help, ButtonSize, "Properties", IconButtonStyle);
            var Delete = CreateIconButton(MaterialIcons.Delete, ButtonSize, "Delete", IconButtonStyle);

            Grid.SetColumn(NewSymbol, 1);
            Grid.SetColumn(NewFolder, 2);
            Grid.SetColumn(EditProperties, 3);
            Grid.SetColumn(Delete, 4);

            footerGrid.Children.Add(NewSymbol);
            footerGrid.Children.Add(NewFolder);
            footerGrid.Children.Add(EditProperties);
            footerGrid.Children.Add(Delete);

            StackLayout_Pane.Children.Add(HzDivider);
            StackLayout_Pane.Children.Add(LibraryDisplayTabs);
            StackLayout_Pane.Children.Add(stackLayout);
            StackLayout_Pane.Children.Add(footerGrid);

            Content = StackLayout_Pane;

        }
    }
}