# HidEnumerator

**Namespace:** `Kwerty.DviZe.Hid.Win`

The Windows implementation of `IHidEnumerator` (`Kwerty.DviZe.Hid`), and the primary entry point for discovering and tracking HID devices.

## Constructor

```csharp
public HidEnumerator(ILoggerFactory loggerFactory);
public HidEnumerator(HidOptions options, ILoggerFactory loggerFactory);
```

### HidOptions

| Property | Type | Default | Description
| --- | --- | --- | ---
| `InstallOnDemand` | `bool` | `true` | Set to `false` to manage installation manually via `InstallAsync`.
| `OnDemandOptions` | `OnDemandOptions`* | `OnDemandOptions.Default`* | Controls the session lifecycle when `InstallOnDemand == true`.
| `MetaDataRetryPolicy` | `RetryPolicy`* | `RetryPolicy.None`* | Retry policy applied when device initialization fails due to an exclusive access conflict.

\*Defined in [DviZe.Common](https://github.com/kwerty/DviZe.Common).

## SubscribeAsync

```csharp
public Task<IDisposable> SubscribeAsync(Action<HidEvent> callback, CancellationToken cancellationToken = default);
public Task<IDisposable> SubscribeAsync(HidEventType filter, Action<HidEvent> callback, CancellationToken cancellationToken = default);
```

Subscribes to HID events.

Dispose the returned `IDisposable` to unsubscribe.

### HidEvent

**Namespace:** `Kwerty.DviZe.Hid`

| Property | Type | Description
| --- | --- | ---
| `Device` | `IHidDevice` | The device that raised the event.
| `EventType` | `HidEventType` | `HidEventType.DeviceMounted` or `HidEventType.DeviceDismounted`.

## InstallAsync

```csharp
public Task InstallAsync(CancellationToken cancellationToken = default);
```

Begins tracking HID devices.

Only valid when `HidOptions.InstallOnDemand == false`.

## DisposeAsync

```csharp
public ValueTask DisposeAsync();
```

Removes all subscriptions and brings the enumerator safely to a close.
