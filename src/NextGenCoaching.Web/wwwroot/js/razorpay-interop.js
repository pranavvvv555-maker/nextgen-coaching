// Minimal JS interop for the Razorpay checkout modal.
// Usage from .NET: await JSRuntime.InvokeAsync<object>("razorpayInterop.openCheckout", options, dotNetRef)
// On success/dismiss the modal calls back into .NET via the supplied object reference.
window.razorpayInterop = {
    openCheckout: function (options, dotNetRef) {
        // Ensure the Razorpay script is on the page (lazy load — much smaller bundle for users who never check out)
        const ensureScript = () => new Promise((resolve, reject) => {
            if (window.Razorpay) return resolve();
            const s = document.createElement("script");
            s.src = "https://checkout.razorpay.com/v1/checkout.js";
            s.async = true;
            s.onload = () => resolve();
            s.onerror = () => reject(new Error("Failed to load Razorpay checkout script"));
            document.head.appendChild(s);
        });

        ensureScript().then(() => {
            const rzpOpts = {
                key: options.razorpayKeyId,
                order_id: options.providerOrderId,
                amount: options.amountInPaise,
                currency: options.currency,
                name: "NextGen Coaching",
                description: options.courseTitle || "Course enrolment",
                prefill: {
                    name:    options.customerName  || "",
                    email:   options.customerEmail || "",
                    contact: options.customerPhone || ""
                },
                theme: { color: "#2563EB" },
                handler: function (response) {
                    // Successful payment — Razorpay returns signed fields. Pass them back to .NET.
                    dotNetRef.invokeMethodAsync("OnRazorpaySuccess",
                        response.razorpay_order_id,
                        response.razorpay_payment_id,
                        response.razorpay_signature);
                },
                modal: {
                    ondismiss: function () {
                        dotNetRef.invokeMethodAsync("OnRazorpayDismissed");
                    }
                }
            };
            try {
                const rzp = new window.Razorpay(rzpOpts);
                rzp.on("payment.failed", function (resp) {
                    const msg = (resp && resp.error && resp.error.description) || "Payment failed";
                    dotNetRef.invokeMethodAsync("OnRazorpayFailed", msg);
                });
                rzp.open();
            } catch (e) {
                dotNetRef.invokeMethodAsync("OnRazorpayFailed", e.message || String(e));
            }
        }).catch(e => {
            dotNetRef.invokeMethodAsync("OnRazorpayFailed", e.message || String(e));
        });
    }
};
