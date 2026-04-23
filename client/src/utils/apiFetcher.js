const apiUrl = import.meta.env.VITE_API_URL;

export const apiRequest = async (url, options, method, navigate) => {
    try {
        const fetchConfig = {
            method: method,
            headers: {
                "Content-Type": "application/json",
            },
            credentials: "include",
        };

        if (options && method !== "GET" && method !== "HEAD") {
            fetchConfig.body = JSON.stringify(options);
        }

        const response = await fetch(apiUrl + url, fetchConfig);

        if (response.status !== 200) {
            console.log("Error occured!\nerror status: " + response.status);
            console.log(response.body);
            navigate("/admin/login");
            return null;
        }

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || `Error: ${response.status}`);
        }

        const contentType = response.headers.get("content-type");

        if (contentType && contentType.includes("application/json")) {
            const json = await response.json();
            return json.data || json;
        }

        return await response.text();
    } catch (error) {
        console.error("Network / server error: ", error);
        return null;
    }
};
