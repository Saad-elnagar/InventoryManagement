(() => {
    const messagesElement = document.getElementById("chat-messages");
    const input = document.getElementById("chat-input");
    const sendButton = document.getElementById("send-message");
    const clearButton = document.getElementById("clear-chat");
    const suggestions = document.querySelectorAll(".ai-suggestion");

    if (!messagesElement || !input || !sendButton) {
        return;
    }

    const history = [];

    const addMessage = (role, content) => {
        history.push({ role, content });

        const wrapper = document.createElement("div");
        wrapper.className = "inventory-ai-message " + role;

        const bubble = document.createElement("div");
        bubble.className = "inventory-ai-bubble";

        if (role === "assistant") {
            const label = document.createElement("div");
            label.className = "inventory-ai-bubble-label";
            label.textContent = "Inventory AI";
            bubble.appendChild(label);
        }

        const text = document.createElement("div");
        text.className = "inventory-ai-bubble-text";
        text.textContent = content;
        bubble.appendChild(text);

        wrapper.appendChild(bubble);
        messagesElement.appendChild(wrapper);
        messagesElement.scrollTop = messagesElement.scrollHeight;
    };

    const setLoading = (loading) => {
        sendButton.disabled = loading;
        input.disabled = loading;

        if (loading) {
            sendButton.innerHTML =
                '<span class="spinner-border spinner-border-sm me-2"></span>Thinking...';

            const wrapper = document.createElement("div");
            wrapper.id = "ai-thinking";
            wrapper.className = "inventory-ai-message assistant";

            const bubble = document.createElement("div");
            bubble.className = "inventory-ai-bubble inventory-ai-thinking";

            bubble.innerHTML =
                '<span></span><span></span><span></span>';

            wrapper.appendChild(bubble);
            messagesElement.appendChild(wrapper);
            messagesElement.scrollTop = messagesElement.scrollHeight;
        } else {
            const thinking = document.getElementById("ai-thinking");

            if (thinking) {
                thinking.remove();
            }

            sendButton.innerHTML =
                '<i class="bi bi-send-fill me-2"></i>Ask AI';

            input.disabled = false;
            input.focus();
        }
    };

    const ask = async () => {
        const question = input.value.trim();

        if (!question || sendButton.disabled) {
            return;
        }

        input.value = "";
        addMessage("user", question);
        setLoading(true);

        try {
            const token =
                document.querySelector(
                    'input[name="__RequestVerificationToken"]')
                    ?.value;

            const response = await fetch(
                "/InventoryAi/Ask",
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": token ?? ""
                    },
                    body: JSON.stringify(history)
                });

            const data = await response.json();

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    "The AI request failed.");
            }

            addMessage(
                "assistant",
                data.answer ||
                "I couldn't generate an answer.");
        } catch (error) {
            addMessage(
                "assistant",
                "AI error: " +
                (error.message ||
                 "The request failed."));
        } finally {
            setLoading(false);
        }
    };

    sendButton.addEventListener("click", ask);

    input.addEventListener("keydown", (event) => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            ask();
        }
    });

    suggestions.forEach(button => {
        button.addEventListener("click", () => {
            input.value = button.textContent.trim();
            input.focus();
        });
    });

    clearButton?.addEventListener("click", () => {
        history.length = 0;

        messagesElement.innerHTML =
            '<div class="inventory-ai-message assistant">' +
                '<div class="inventory-ai-bubble">' +
                    '<div class="inventory-ai-bubble-label">Inventory AI</div>' +
                    'New chat started. Ask me about your inventory.' +
                '</div>' +
            '</div>';

        input.value = "";
        input.focus();
    });

    input.focus();
})();
