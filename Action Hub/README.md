Action Hub is a centralized management window for Unity. Its goal is not to replace existing windows where you do your work, but rather to help you find the places and objects you are working on. By remembering your most recent actions, Action Hub reduces the amount of clicking you need to do to get to the place you need to work.

There were three primary motivations for Action Hub, first was the observation that when working on a specific feature is that you will only be working with a handful of assets. Each feature might see you working with different assets. There are assets out there that allow you to define bookmarks, or even groups of related assets, but for me the setup of these assets detracts from getting the work done. Furthermore, since the work is ever-changing the setup is never quite right. Action Hub takes a different approach, it automatically tracks the folders and assets you have clicked on recently, making them easily re-discoverable as you switch between them during feature development. When you move on to a new feature you don't have to setup Action Hub to track new features, it will notice you have started working with other assets and follow along. In practice, what this means is that you use Unity just as you always have, while Action hub provides an ever-evolving set of shortcuts to the assets that are most important to you right now.

The second motivation was that tracking the jobs to be done for a feature to be complete requires simple notes, not a full blown issue tracker. Action hub provides a simple ToDo tracker that allows you to keep these notes right in unity, complete with links to objects that are applicable to that task. But hold on, you might ask "isn't that the setup you wanted to avoid in the first motivator?" Well no. What I find is that while I'm working on another part of the feature I'll see something else that needs to be done. I click on the asset, it appears in my list of recent items and I can write a simple ToDo not right there, click a button and done, there's my ToDo item conveniently recorded with a link to the asset that sparked the thought in the first place. Naturally the ToDo list can hold more notes if you need them, but generally the one-liner and an asset link are all that are needed to stay in the flow, while keeping your notes. 

The third primary motivation was that as a project grows there is an ever growing set of data assets that need to maintained. Ensuring the settings within these assets evolve with code changes can be error prone and time consuming. Action Hub provides a validation framework that allows you to write arbitrary validation code in `MonoBehaviour`s. This validation code will be run periodically and things that need your attention will be automatically added to the action hub. This same validation code can be used in `OnValidate` too, giving you a simple and consistent approach to quality control across standard Unity tooling and Action Hub.

It doesn't stop there though, Action Hub is designed to be extensible. You can create custom categories of actions that will expand Action Hub for your specific needs. Two additional examples that are included are Scene Actions which allow you to quickly access your scene folders while setting default behaviours for them upon opening (e.g. additive or unique). If you have ideas for additional actions to include in Action Hub please let us know in our Discord (http://bit.ly/WizardsCodeDiscord).

# Action Hub Window

To open the Action Hub use `Tools -> Wizards Code -> Action Hub`. The window can be resized and/or docked as you best suits your workflow.

ToDo: The sections of the Window
ToDo: Recent Folders and items 
ToDo: Action Lists

# Action Categories

Action Categories are used to automatically group similar actions in the UI. You can create an arbitrary number of categories to suit your own work style. As you will see later you can also create specialist Action classes to populate these categories (requires coding).

Categories can be used in many ways, for example, if you have a particularly complex feature you are working on you could create a dedicated Category for that feature. Of you might want to configure a launch checklist category that will auto-populate with tasks related to your release process. Categories, with the ability to code unique actions for them are exceptionally powerful.

To create a new Action Category use `Create -> Wizards Code -> Action Hub -> Category`. Action Categories must be located in a `Resources` folder, but can be in any directory within. Action Hub automatically creates a folder `Assets/Wizards Code/User Data/Editor/Resources/Action Hub` for storing data. It is therefore a good idea to create a `Categories` folder within there and use that.

Note that, by default, categories will not show in your UI unless they have active actions within them. This is useful if the category is auto-populated. However, if you want to manual populate a category you will want it to always be visible. There's a checkbox for this purpose - don't miss it ;-)

Once you have created a new category right click on the Action Hub window and select `Refresh Data` to ensure it appears in the UI.

Categories can have one or more Action Templates assigned to them. These will present a GUI element for creating action items within the category. See the Action Templates section below for more information.

We try to ensure that all fields for objects have meaningful and useful tooltips, be sure to read them as you learn about the features of Action Hub.

## Developer Notes

Action Categories are defined in `WizardsCode.ActionHubEditor.ActionCategory`. The GUI is generated in the `ActioListGUI` method. If you want to define custom actions for a category, as opposed to actions within a category, extend this class and override this method.

# Actions

Actions are the most important part of the Action Hub. They define the actions that you want to track. Action Hub comes with a good set of common actions, but if you are able to write code you can create your own actions with potentially complex behaviours.

The included actions are:

  * Generic Action - the simplest of actions, little more than a note.
  * ToDo Action - a note, a status (done or not) and an optional set of linked assets. Usually for quickly tracking things that need to be done.
  * Scene - this action allows you to load (additively or otherwise) or play scenes, without hunting through your project.
  * Validation Action - this is a very powerful action that can really help with maintaining the quality of your project. In short it runs a number of automated and custom tests, reporting the results. 
  * Countdown Timer - useful for anyone who like to use the Pomodoro or similar time management process. Or perhaps you want to limit your playtesting session to 60 minutes. This action will help.

In most cases you will create an action from within the Category you ant to work in within the Action Hub. Categories can have one or more templates that define the actions that can be created from within the UI. See the Action Templates section below for more information. You can also create actions from withn the Unity Project window using `Create -> Wizards Code -> Action Hub -> [ACTION TYPE]`.

## Developer Notes

### Custom Actions

Creating your own action type is easy. Simply extend one of the other action types, or the base `Action` class.

The UI for an Action is generated in the `OnCustomUI`. Sometimes you will also need to override `OnEndGUI` which is called immediately after `OnCustomUI`, but generally you can leave this as-is.

If you want to be able to create templates to allow users to create instances of your action from within the Action Hub then you will need to provide a customer create GUI in `OnCreateGUI`.

If your action needs to run periodically then you can override `OnUpdate` which is a intended to be a coroutine, meaning you have some control of ensuring your code is efficient and doesn't block the editor. However, for performance reasons this coroutine is not started by default. If you want it to always be running then there is a flag in the Action Object to enable this, but more frequently it will be started manually by the user interacting with the Action in some way.

### Validation Actions

TODO: Complete Wizard Attributes integration and document

In order for the Validation Action to validate you Components and Scriptable Objects they need to implement a method called `IsValid`. This can be an extension method if you do not have direct access to the source code, such as in a purchased asset that uses DLL's. This method should return true if the object is valid and false if not. It can also return an error message in the event of a failure, for example:

```csharp
public bool IsValid(out string message)
{
    // Check if the negativeValue is less than zero
    if (negativeValue >= 0)
    {
        message = "The 'negativeValue' must be greater than zero.";
        return false;
    }

    // Check if the positiveValue is greater than zero
    if (positiveValue <= 0)
    {
        message = "The 'positiveValue' must be greater than zero.";
        return false;
    }

    // If all checks pass
    message = "Validation passed.";
    return true;
}
```

# Action Templates

Action Templates are special actions that provide default settings for a particular type of action. One of these defaults will always be the Action Category. Templates will always be presented in the GUI for the category, allowing new actions to be created based on that template. Each category can have any number of templates.

To create an Action Template simply Create a new Action (using `Create -> Wizards Code -> Action Hub -> Action`). Be sure to give it a name that ends with "Template", or it will not be recognized as a template. Set default values and the Category the template belongs to and refresh the dthe Action Hub data by right clicking on the window and selecting `Refresh Data`.

Templates can be stored in any subdirectory of a resources folder. We recommend using `Assets/Wizards Code/User Data/Editor/Resources/Action Hub/Templates`.




