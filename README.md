# Notifliwy - Fluent Event Notification Library for .NET

![banner](contents/repo.banner.png)

---

**Notifliwy** is a _powerful_ .NET library designed 
to **simplify** **event-driven** architecture implementation with a clean, `fluent API`. 
It provides end-to-end support for event notifications - 
from declaration to processing - using an intuitive **builder** pattern that makes complex 
workflows easy to configure.

---

## Key Features

* 🚀 **Fluent** builder API for seamless configuration

* 🧩 **Modular pipeline** components for event processing

* ⏱️ Built-in support for async processing

* 📦 **Dependency Injection** ready

* 📊 **Metrics** and **diagnostics** out of the box

## Stages of event processing

> `event` -> `connector<event>` -> `handler.executor<event>` 
> -> `notification.handler<notification, event>` -> `condition<notification, event>` 
> -> `mapper<notification, event>` -> `exporter<notification>`

1. `InputPipe<TEvent>` - input service, `AcceptAsync` return `IAsyncEnumerable` with assigned **events**

---

## Project

This project comes with an [Apache license](LICENSE). Contact the `.stbl` group.