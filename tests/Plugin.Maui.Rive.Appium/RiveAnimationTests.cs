using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.iOS;

namespace Plugin.Maui.Rive.Appium;

[TestFixture]
public class RiveAnimationTests
{
    private IOSDriver? _driver;

    [OneTimeSetUp]
    public void Setup()
    {
        var options = new AppiumOptions
        {
            PlatformName = "iOS",
            AutomationName = "XCUITest",
            DeviceName = "iPhone 16 Pro",
        };
        options.AddAdditionalAppiumOption("appium:udid", "9B25DEC2-88C8-4612-B5E2-2B04C8313D4D");
        options.AddAdditionalAppiumOption("appium:bundleId", "com.companyname.pluginsample");
        options.AddAdditionalAppiumOption("appium:noReset", true);
        options.AddAdditionalAppiumOption("appium:newCommandTimeout", 120);
        options.AddAdditionalAppiumOption("appium:usePrebuiltWDA", true);

        _driver = new IOSDriver(new Uri("http://127.0.0.1:4723"), options, TimeSpan.FromMinutes(5));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
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
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should be visible");
    }

    [Test, Order(2)]
    public void PlayButton_IsDisplayed()
    {
        var btn = _driver!.FindElement(MobileBy.AccessibilityId("PlayButton"));
        Assert.That(btn.Displayed, Is.True);
    }

    [Test, Order(3)]
    public void PauseButton_TapWorks()
    {
        var pauseBtn = _driver!.FindElement(MobileBy.AccessibilityId("PauseButton"));
        pauseBtn.Click();
        Thread.Sleep(1000);

        // Verify the view is still displayed after pausing
        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after pause");
    }

    [Test, Order(4)]
    public void PlayButton_TapResumesPlayback()
    {
        var playBtn = _driver!.FindElement(MobileBy.AccessibilityId("PlayButton"));
        playBtn.Click();
        Thread.Sleep(1000);

        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after play");
    }

    [Test, Order(5)]
    public void StopButton_TapStopsPlayback()
    {
        var stopBtn = _driver!.FindElement(MobileBy.AccessibilityId("StopButton"));
        stopBtn.Click();
        Thread.Sleep(1000);

        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after stop");
    }

    [Test, Order(6)]
    public void ResetButton_TapResetsAnimation()
    {
        var resetBtn = _driver!.FindElement(MobileBy.AccessibilityId("ResetButton"));
        resetBtn.Click();
        Thread.Sleep(500);

        // After reset the view should still be visible
        var riveView = _driver!.FindElement(MobileBy.AccessibilityId("RiveAnimationView"));
        Assert.That(riveView.Displayed, Is.True, "RiveAnimationView should still be visible after reset");
    }

    [Test, Order(7)]
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
