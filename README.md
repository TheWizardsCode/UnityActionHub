# UnityActionHub
A Unity Editor Plugin to simplify common actions in Game Development.

# Features

  * Automatically track the last 10 folders and items selected in the project window for easy recall
  * Intelligently create quick options for appropriate items, e.g. play a sound file, load a scene
  * ToDo manager for tracking immediate work items, intended as a complement to a full task management system
  * Meaningful, and useful, tooltips providing easy access to essential information about actions and items tracked
  * Reduce the need to open additional inspectors and lock them when working across folders
  * Extensible system to allow you to build your own actions optimized for tasks in your process

# Use

Open the window with `Tools -> Wizards Code -> Action Hub`.

Sections will automatically populate as you add content into your project. The sections available include. In almost all
sections single clicking on an items label will ping the item in the project window. Double-clicking will open the item. 

## Scenes

Gather a list of scenes in your build and perform common actions on them, such as load, unload and play.

You can create new scene references by selecting the scene from the dropdown and clicking the `Add Scene` button. Only scenes available in your Build Settings are available in the dropdown.

You can also create scene actions by right clicking in any `Resources` folder and selecting 
`Create -> Wizards Code -> Action Hub -> Scene`

## ToDo

This is not a full blown task management system. It is, however, a reasonably powerful task management system built 
right into Unity. It is intended as a lightweight addition to your primary project management tool - whatever that 
might be. You might pick up a major work item from your primary system and then track smaller tasks within Unity.

Hovering over a ToDo item will show its full description while double clicking it will open the item in the 
inspector where it can be freely editing.

Create new ToDo items directly within the Action Hub, simply type in a short description and click `Add Todo`. 
You can also create items by right clicking inside any Resources folder and selecting 
`Create -> Wizards Code -> Action Hub -> ToDo`

## Custom actions

Action Hub is designed to be highly flexible. This means you can add your own actions easily to the system. Let's
look at how we would add a Pomodoro Timer. You can follow along if you like, but the final results of this process
are included in the asset.

The [Pomodoro Technique](https://en.wikipedia.org/wiki/Pomodoro_Technique) is a time management method that breaks 
work into intervals, typically 25 minutes in length, separated by short breaks. A Pomodoro Timer is a device that
keeps you on track with respect to these times sessions.

Rather than making something that is specific to a Pomodoro Timer, lets make a more generic countdown time that
can be used for other purposes too.

  * Create a new class called `CountdownTimer`, make it extend the `Action` class. This is a ScriptableObject which will hold all the data about your item.
  * Add an `CreateAssetMenu` attribute as you would for any other Scriptable Object.

We can now create our first timer, though it won't actually countdown yet. Go ahead and right click inside any Resources folder and selecting 
`Create -> Wizards Code -> Action Hub -> Countdown Timer` (or whatever name you used in the CreateAssetMenu attribute). Note that you have some
useful fields available to you already. Fill these out with whatever values you want, we are going to call ours "Pomodoro Timer" and give a short 
description.

Now we need to give it some functionality. Lets first create a single timer. For this we will need a duration field. This is added like any other
field in a `ScriptableObject`.

```csharp
[SerializeField, Tooltip("The duration of the countdown in seconds.")]
private float m_Duration = 1500; // 25 minutes
```

We will also need a button for starting and stopping the timer. There are two virtual methods for you to use to create the GUI, though you will usually 
only need the first. `OnCustomGUI` and `OnEndGUI`. The first is inteded to contain action specific UI, while the second contains common elements that 
appear most items. Create your `OnCustomGUI` as follows:

```csharp
        protected override void OnCustomGUI()
        {
            GUILayout.BeginHorizontal();
            {
                ActionHubWindow.CreateClickableLabel(DisplayName, Description, this);

                if (m_IsRunning)
                {
                    ActionHubWindow.CreateLabel($"Time remaining: {(m_EndTime - Time.realtimeSinceStartup).ToString("F0")}");
                }

                if (!m_IsRunning && GUILayout.Button($"Start {m_Duration.ToString("F0")} second timer"))
                {
                    m_EndTime = Time.realtimeSinceStartup + m_Duration;
                    m_IsRunning = true;
                    // ActivateOnUpdate();
                }
            }
            GUILayout.EndHorizontal();
        }
```

At this point our timer will start when we click the button. However, it only updates when the mouse is over the window, which is not ideal if you want to be notified when it has finished. We can resolve this by 
overriding the `OnUpdate()` method:

Of course, we also need to implement the `OnUpdate` method:

```csharp
        protected override IEnumerator OnUpdate()
        {
            WaitForSeconds waitOneSecond = new WaitForSeconds(1);

            while (m_IsRunning)
            {
                if (Time.realtimeSinceStartup > m_EndTime)
                {
                    m_IsRunning = false;
                }

                ActionHubWindow.ForceRepaint();

                yield return waitOneSecond;
            }

            ActionHubWindow.ShowConfirmationDialog(DisplayName + " Complete", $"{DisplayName} has finished.");
        }
```

Note that OnUpdate is not automatically started by default. This is for performance reasons. You can set `ActivateOnUpdateAtStart` to true on all the implementations, 
but this routine only needs to run when there is an active timer, so we should start it when the user clicks the start button. Uncomment the following line
in the Button action above:

```csharp
ActivateOnUpdate();
```

Now, when we start the timer will begin its countdown and when it reaches completion it will open a dialog box. This isn't a true Pomodoro Timer yet since it only has 
a single timer, a true Pomodoro Timer would follow this up with a shorter 5 minute timer. We'll not add this functionality in this tutorial as it is really more of the
same. The implementation in the project has this feature, so if you want to see one way of doing it, take a look.

It's a little inconvenient having to create a scriptable object in your project whenever you want a new timer though, and we don't like inconvenience. So let's go ahead and implement a
GUI that allows the creation of an instance of this Countdown action from within the ActionHubWindow. To do this we need to add a little custom code to the `CountdownTimer` 
class, as follows:

```csharp
        internal override void OnCreateGUI()
        {
            GUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField("Create Countdown", GUILayout.Width(CreateLabelWidth));

                DisplayName = EditorGUILayout.TextField(DisplayName, GUILayout.ExpandWidth(true));

                EditorGUILayout.LabelField("Duration (seconds)", GUILayout.Width(150));
                Duration = EditorGUILayout.FloatField(Duration, GUILayout.Width(75));

                if (m_Duration < 0)
                {
                    m_Duration = 0;
                }

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(DisplayName));
                if (GUILayout.Button("Add", GUILayout.Width(100)))
                {
                    // create a new ToDoAction and save it to the AssetDatabase
                    CountdownTimer newAction = ScriptableObject.CreateInstance<CountdownTimer>();
                    newAction.DisplayName = DisplayName;
                    newAction.Duration = Duration;
                    newAction.Category = Category;

                    // save the new asset as a child of this category
                    AssetDatabase.AddObjectToAsset(newAction, this);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorGUIUtility.PingObject(newAction);

                    // Reset the template values
                    DisplayName = string.Empty;
                    Duration = 1500;

                    ActionHubWindow.RefreshActions();
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }
```

This code will create a creation UI. However, we still need to tell the Action Hub Window where to display 
it and what the default settings are for any timer we create. Both of these things are done by creating a
template timer. Create a new instance of the Countdown Time Scriptable Object by right clicking on a 
resource folder and selecting `Create -> Wizards Code -> Action Hub -> Countdown Timer`. Give it a name
that ends in "Template", this naming convention is very important. The start of the name can be anything
you want, but the last part must be Template. This tells the Action Hub that it isn't really an action, but
rather a template from which other actions will be created.

Setup this template however you want. Pay special attention to the category setting. This will dictate where
the create UI for this template will appear in the Action Hub. Note that you can create multiple templates
with different default settings and have them appear in different categories if you want to.

For now assign the `ToDo Action Category`. Once you do this your Create UI will appear under the ToDo heading
in the action hub. Any timers you create using this template will appear in the list of actions under this 
category. You can, however change the category in the inspector if you want it to appear somewhere else.

## Creating Categories

Categories are very important when trying to stay organized. So what if you don't want your timers cluttering
up the ToDo section. After all, once they are created they are not very important. Lets create a new category,
called `Time Management` that will hold your timers. This is super easy to do.

Right click in a resources folder and select `Create -> Wizards Code -> Action Hub -> Category`. Name the file
"Time Management" or whatever you want. Fill in the details in the inspector and you are done. The only truly
critical item is the `Display Name` which is used in the UI. All items should have tooltips explaining their
purpose. In this instance you might want to make the priority much higher, say 10000. This acts as a kind of 
sort order. The higher the number the further down the Action Hub Window it will appear.

To make your template use this category simply change it in the template itself. Once you do this the Create UI
and any new timers created will appear in this new category.