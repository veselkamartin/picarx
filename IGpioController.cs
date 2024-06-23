using System.Device.Gpio;
public class GpioController2 : GpioController, IGpioController { }

public interface IGpioController {
   
  
    /// <summary>
    /// Opens a pin in order for it to be ready to use.
    /// The driver attempts to open the pin without changing its mode or value.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
     GpioPin OpenPin(int pinNumber);


    /// <summary>
    /// Opens a pin and sets it to a specific mode.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="mode">The mode to be set.</param>
    GpioPin OpenPin(int pinNumber, PinMode mode);

    /// <summary>
    /// Opens a pin and sets it to a specific mode and value.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="mode">The mode to be set.</param>
    /// <param name="initialValue">The initial value to be set if the mode is output. The driver will attempt to set the mode without causing glitches to the other value.
    /// (if <paramref name="initialValue"/> is <see cref="PinValue.High"/>, the pin should not glitch to low during open)</param>
    GpioPin OpenPin(int pinNumber, PinMode mode, PinValue initialValue);

    /// <summary>
    /// Closes an open pin.
    /// If allowed by the driver, the state of the pin is not changed.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    void ClosePin(int pinNumber);



    /// <summary>
    /// Sets the mode to a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="mode">The mode to be set.</param>
     void SetPinMode(int pinNumber, PinMode mode);

    /// <summary>
    /// Gets the mode of a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <returns>The mode of the pin.</returns>
    PinMode GetPinMode(int pinNumber);

    /// <summary>
    /// Checks if a specific pin is open.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <returns>The status if the pin is open or closed.</returns>
    bool IsPinOpen(int pinNumber);

    /// <summary>
    /// Checks if a pin supports a specific mode.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="mode">The mode to check.</param>
    /// <returns>The status if the pin supports the mode.</returns>
    bool IsPinModeSupported(int pinNumber, PinMode mode);

    /// <summary>
    /// Reads the current value of a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <returns>The value of the pin.</returns>
    PinValue Read(int pinNumber);

    /// <summary>
    /// Toggle the current value of a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    void Toggle(int pinNumber);

    /// <summary>
    /// Writes a value to a pin.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="value">The value to be written to the pin.</param>
    void Write(int pinNumber, PinValue value);

    /// <summary>
    /// Blocks execution until an event of type eventType is received or a period of time has expired.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="eventTypes">The event types to wait for.</param>
    /// <param name="timeout">The time to wait for the event.</param>
    /// <returns>A structure that contains the result of the waiting operation.</returns>
    WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, TimeSpan timeout);

    /// <summary>
    /// Blocks execution until an event of type eventType is received or a cancellation is requested.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="eventTypes">The event types to wait for.</param>
    /// <param name="cancellationToken">The cancellation token of when the operation should stop waiting for an event.</param>
    /// <returns>A structure that contains the result of the waiting operation.</returns>
    WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken);

    /// <summary>
    /// Async call to wait until an event of type eventType is received or a period of time has expired.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="eventTypes">The event types to wait for.</param>
    /// <param name="timeout">The time to wait for the event.</param>
    /// <returns>A task representing the operation of getting the structure that contains the result of the waiting operation.</returns>
    ValueTask<WaitForEventResult> WaitForEventAsync(int pinNumber, PinEventTypes eventTypes, TimeSpan timeout);

    /// <summary>
    /// Async call until an event of type eventType is received or a cancellation is requested.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="eventTypes">The event types to wait for.</param>
    /// <param name="token">The cancellation token of when the operation should stop waiting for an event.</param>
    /// <returns>A task representing the operation of getting the structure that contains the result of the waiting operation</returns>
    ValueTask<WaitForEventResult> WaitForEventAsync(int pinNumber, PinEventTypes eventTypes, CancellationToken token);

    /// <summary>
    /// Adds a callback that will be invoked when pinNumber has an event of type eventType.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="eventTypes">The event types to wait for.</param>
    /// <param name="callback">The callback method that will be invoked.</param>
    void RegisterCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback);

    /// <summary>
    /// Removes a callback that was being invoked for pin at pinNumber.
    /// </summary>
    /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
    /// <param name="callback">The callback method that will be invoked.</param>
    void UnregisterCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback);



    /// <summary>
    /// Write the given pins with the given values.
    /// </summary>
    /// <param name="pinValuePairs">The pin/value pairs to write.</param>
    void Write(ReadOnlySpan<PinValuePair> pinValuePairs);

    /// <summary>
    /// Read the given pins with the given pin numbers.
    /// </summary>
    /// <param name="pinValuePairs">The pin/value pairs to read.</param>
    void Read(Span<PinValuePair> pinValuePairs);

   
}

