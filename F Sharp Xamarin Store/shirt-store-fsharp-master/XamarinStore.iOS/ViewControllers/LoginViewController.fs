﻿namespace XamarinStore

open System
open MonoTouch.UIKit
open BigTed
open MonoTouch.Foundation

type LoginViewController() as this =
    inherit UIViewController()

    // TODO Add your Xamarin account email address here (sign up at store.xamarin.com/account/register)
    //
    // In the C# version of this app, xamarinAccountEmail is just a string, with a default value of ""
    // to indicate that no email address has been entered. F# has a type called option<'T> precisely for
    // situations when a value may not be present. Here, xamarinAccountEmail is of type option<string>.
    // Add your email by replacing None with Some "email@domain.com":
    //
    //    let xamarinAccountEmail = Some "email@domain.com"
    //
    let xamarinAccountEmail = None

    let mutable ContentView:UIView = null
    let mutable LoginView = null
    let mutable scrollView : UIScrollView = null
    let mutable keyboardOffset = 0.0f

    do this.Title <- "Log in"
       //This hides the back button text when you leave this View Controller
       this.NavigationItem.BackBarButtonItem <- new UIBarButtonItem ("", UIBarButtonItemStyle.Plain, handler=null)
       this.AutomaticallyAdjustsScrollViewInsets <- false


    let OnKeyboardNotification (notification:NSNotification) =
        if this.IsViewLoaded then
            //Check if the keyboard is becoming visible
            let visible = notification.Name = UIKeyboard.WillShowNotification.ToString()
            UIView.Animate (UIKeyboard.AnimationDurationFromNotification (notification), 
                            fun () -> UIView.SetAnimationCurve ( (box (Convert.ToInt32(UIKeyboard.AnimationCurveFromNotification(notification)))) :?> UIViewAnimationCurve)
                                      let frame = UIKeyboard.FrameEndFromNotification notification
                                      keyboardOffset <- if visible then frame.Height else 0.0f
                                      this.ViewDidLayoutSubviews ())

    member val LoginSucceeded = fun () -> () with get,set
   
    override this.ViewDidLayoutSubviews () =
        let mutable bounds = this.View.Bounds
        ContentView.Frame <- bounds
        scrollView.ContentSize <- bounds.Size
        //Resize Scroller for keyboard
        bounds.Height <- bounds.Height - keyboardOffset
        scrollView.Frame <- bounds

    override this.LoadView () =
        base.LoadView ()
        scrollView <- new UIScrollView (this.View.Bounds)
        this.View.AddSubview scrollView
        match xamarinAccountEmail with
        | Some email ->
            LoginView <- new LoginView(email)
            LoginView.UserDidLogin <- fun _ ->
                this.Login email LoginView.PasswordField.Text |> Async.StartImmediate
            ContentView <- LoginView
            scrollView.Add ContentView
        | None ->
            ContentView <- new PrefillXamarinAccountInstructionsView ()
            scrollView.Add(ContentView)


    override this.ViewWillAppear animated =
        base.ViewWillAppear animated
        NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardNotification) |> ignore
        NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardNotification) |> ignore

    override this.ViewWillDisappear animated =
        base.ViewWillDisappear (animated);
        NSNotificationCenter.DefaultCenter.RemoveObservers [|UIKeyboard.WillHideNotification; UIKeyboard.WillShowNotification|]

    member this.Login username password = async {
        BTProgressHUD.Show ("Logging in...")

        let! success = WebService.Shared.Login username password
        if success then
            let! canContinue = WebService.Shared.PlaceOrder(WebService.Shared.CurrentUser, true)
            BTProgressHUD.Dismiss ()
            if not canContinue.Success 
            then (new UIAlertView("Sorry", "Only one shirt per person. Edit your cart and try again.", null, "OK")).Show()
            else this.LoginSucceeded ()
        else
            BTProgressHUD.Dismiss ()
            let alert = new UIAlertView ("Could Not Log In", "Please verify your Xamarin account credentials and try again", null, "OK");
            alert.Show ()
            alert.Clicked.Add(fun _ -> LoginView.PasswordField.SelectAll this
                                       LoginView.PasswordField.BecomeFirstResponder() |> ignore ) }

