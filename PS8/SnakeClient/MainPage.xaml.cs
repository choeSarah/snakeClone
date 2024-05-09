namespace SnakeGame;

public partial class MainPage : ContentPage
{
    private GameController gc;
    public MainPage()
    {
        InitializeComponent();

        gc = new GameController();

        worldPanel.SetWorld(gc.GetWorld());

        //graphicsView.Invalidate();

        gc.UpdateArrived += OnFrame;
        gc.ErrorArrived += NetworkErrorHandler;
    }

    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")
        {
            // Move up
            gc.MoveUp();
        }
        else if (text == "a")
        {
            // Move left
            gc.MoveLeft();
        }
        else if (text == "s")
        {
            // Move down
            gc.MoveDown();
        }
        else if (text == "d")
        {
            // Move right
            gc.MoveRight();
        }
        entry.Text = "";

        keyboardHack.Focus();
    }

    private void NetworkErrorHandler()
    {
        DisplayAlert("Error", "Disconnected from server", "OK");
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }

        //CODE TO START THE CONTROLLER'S CONNECTING PROCESS
        //DisplayAlert("Delete this", "Code to start the controller's connecting process goes here", "OK");

        bool connected = gc.Connect(serverText.Text, nameText.Text);

        if (connected == false)
        {
            DisplayAlert("Error", "Couldnt' connect to server", "OK");
        } else
        {
            connectButton.IsEnabled = false;
            serverText.IsEnabled = false;

        }

        keyboardHack.Focus();
    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        //worldPanel.SetWorld(gc.GetWorld());
        Dispatcher.Dispatch(() => graphicsView.Invalidate());

        keyboardHack.Focus();
    }

    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");

        keyboardHack.Focus();
    }

    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by ...\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");

        keyboardHack.Focus();
    }

    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }
}