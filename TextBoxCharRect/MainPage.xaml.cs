using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestCharRect
{
   /// <summary>
   /// An empty page that can be used on its own or navigated to within a Frame.
   /// </summary>
   public sealed partial class MainPage : Page
   {
      public MainPage()
      {
         this.InitializeComponent();

         this.Abc.TextChanged += this.Abc_TextChanged;
         this.Abc.LosingFocus += this.Abc_LosingFocus;
         this.Abc.LostFocus += this.Abc_LostFocus;
         this.Abc.PreviewKeyDown += this.Abc_PreviewKeyDown;

         this.Abc.SelectionChanging += this.Abc_SelectionChanging;

         this.Slider.ValueChanged += this.Slider_ValueChanged;

         this.UpdateSliderMaximum();
         this.UpdateTextRect();
      }

      private void Abc_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
      {

         switch (e.Key)
         {
            case Windows.System.VirtualKey.Left:
               this.Slider.Value--;

               e.Handled = true;
               break;
            case Windows.System.VirtualKey.Right:
               this.Slider.Value++;

               e.Handled = true;
               break;
               //case Windows.System.VirtualKey.Up:
               //case Windows.System.VirtualKey.Down:
               //   e.Handled = true;
               //   break;
         }
      }

      private void UpdateTextRect()
      {

         if (this.Abc.Text.Length == 0)
         {
            this.CharRect.Visibility = Visibility.Collapsed;
            this.Slider.IsEnabled = false;
         }
         else
         {
            this.CharRect.Visibility = Visibility.Visible;
            this.Slider.IsEnabled = true;

            int index = Math.Max(0, (int)this.Slider.Value - 1);

            Rect leadingEdge = this.Abc.GetRectFromCharacterIndex(index, trailingEdge: false);
            Rect trailingEdge = this.Abc.GetRectFromCharacterIndex(index, trailingEdge: true);

            if (trailingEdge.Height == 0)
            {
               trailingEdge = leadingEdge;
            }

            // The true rectangle is built out of the two rectanges we have incoming
            Canvas.SetLeft(this.CharRect, leadingEdge.Left);
            Canvas.SetTop(this.CharRect, leadingEdge.Top);

            this.CharRect.Height = trailingEdge.Height / 2 + 2;
            this.CharRect.Width = Math.Max(0, trailingEdge.Left - leadingEdge.Left) + 2;

            // TODO: Animate?
         }
      }

      bool updatingSelection = false;
      private void UpdateInsertionPoint()
      {

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

         // Push this to the next dispatcher cycle, as otherwise we can end up in a case where we cancelled the natural selection,
         // in order to cycle-break the slider-induced selection (natural selection works, in effect, by updating the slider, then allowing
         // slider-induced selection to run), which will also swallow the slider-induced one.
         Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
         {
            this.updatingSelection = true;
            this.Abc.Select((int)this.Slider.Value, 0);
            this.updatingSelection = false;
         });

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
      }

      private void Abc_SelectionChanging(TextBox sender, TextBoxSelectionChangingEventArgs args)
      {
         if (!this.updatingSelection)
         {
            args.Cancel = true;
            this.Slider.Value = args.SelectionStart + args.SelectionLength;
         }
      }

      private void UpdateSliderMaximum()
      {
         int newMax = Math.Max(0, this.Abc.Text.Length);

         // Check for processingTextChanged, because that triggers a SelectionChanging before the TextChanged
         // event fires. This means that our logic for deciding whether to update the slider position is wrong,
         // as it now already points as the new position. This fixes a bug where typing from one character
         // behind leading would cause insertion point to jump to end.
         //
         // Note: One would expect a similar weirdness to occur when deleting a character, but this does not 
         // trigger a problem with mistimed reselection, because the newly updated maximum clamps the selection.
         bool updateSlider = !this.processingTextChanged && this.Slider.Value == this.Slider.Maximum;
         this.Slider.Maximum = newMax;
         if (updateSlider) this.Slider.Value = newMax;
      }

      bool processingTextChanged = false;
      private void Abc_TextChanged(object sender, TextChangedEventArgs e)
      {
         this.processingTextChanged = true;
         this.UpdateSliderMaximum();
         this.processingTextChanged = false;
      }

      private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
      {
         this.UpdateTextRect();
         this.UpdateInsertionPoint();
      }

      private void Abc_LosingFocus(UIElement sender, LosingFocusEventArgs args)
      {
         if (args.NewFocusedElement != null) args.TryCancel();
      }

      private void Abc_LostFocus(object sender, RoutedEventArgs e)
      {
         this.Abc.Focus(FocusState.Programmatic);
      }
   }
}
