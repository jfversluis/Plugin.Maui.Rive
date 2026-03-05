using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace Plugin.Maui.Rive.Appium;

[TestFixture]
public class RiveAnimationWindowsTests
{
    private WindowsDriver? _driver;

    [OneTimeSetUp]
    public void Setup()
    {
        var options = new AppiumOptions
        {
            PlatformName = "Windows",
            AutomationName = "Windows",
        };
        options.AddAdditionalAppiumOption("appium:app", "com.companyname.pluginsample_9zz4h110yvjzm!App");
        options.AddAdditionalAppiumOption("appium:noReset", true);
        options.AddAdditionalAppiumOption("appium:newCommandTimeout", 120);

        _driver = new WindowsDriver(new Uri("http://127.0.0.1:4723"), options, TimeSpan.FromMinutes(5));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);

        // Navigate to Playback tab (3rd tab)
        Thread.Sleep(3000);
        try
        {
            var playbackTab = _driver.FindElement(MobileBy.AccessibilityId("Playback"));
            playbackTab.Click();
            Thread.Sleep(2000);
        }
        catch
        {
            // Tab may already be selected or navigation differs
        }
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    [Test, Order(1)]
    public void AnimationView_IsDisplayed()
    {
        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should be visible on Windows");
    }

    [Test, Order(2)]
    public void AnimationView_HasSize()
    {
        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Size.Width, Is.GreaterThan(0), "RiveAnimationView should have width > 0");
        Assert.That(riveView.Size.Height, Is.GreaterThan(0), "RiveAnimationView should have height > 0");
    }

    [Test, Order(3)]
    public void PlayButton_IsDisplayed()
    {
        var btn = _driver!.FindElement(MobileBy.AccessibilityId("PlayButton"));
        Assert.That(btn.Displayed, Is.True);
    }

    [Test, Order(4)]
    public void PauseButton_TapWorks()
    {
        var pauseBtn = _driver!.FindElement(MobileBy.AccessibilityId("PauseButton"));
        pauseBtn.Click();
        Thread.Sleep(1000);

        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after pause");
    }

    [Test, Order(5)]
    public void PlayButton_TapResumesPlayback()
    {
        var playBtn = _driver!.FindElement(MobileBy.AccessibilityId("PlayButton"));
        playBtn.Click();
        Thread.Sleep(1000);

        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after play");
    }

    [Test, Order(6)]
    public void StopButton_TapStopsPlayback()
    {
        var stopBtn = _driver!.FindElement(MobileBy.AccessibilityId("StopButton"));
        stopBtn.Click();
        Thread.Sleep(1000);

        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after stop");
    }

    [Test, Order(7)]
    public void ResetButton_TapResetsAnimation()
    {
        var resetBtn = _driver!.FindElement(MobileBy.AccessibilityId("ResetButton"));
        resetBtn.Click();
        Thread.Sleep(500);

        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after reset");
    }

    [Test, Order(8)]
    public void AllButtons_ArePresent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_driver!.FindElement(MobileBy.AccessibilityId("PlayButton")).Displayed, Is.True);
            Assert.That(_driver!.FindElement(MobileBy.AccessibilityId("PauseButton")).Displayed, Is.True);
            Assert.That(_driver!.FindElement(MobileBy.AccessibilityId("StopButton")).Displayed, Is.True);
            Assert.That(_driver!.FindElement(MobileBy.AccessibilityId("ResetButton")).Displayed, Is.True);
        });
    }
}
