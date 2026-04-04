import { BrowserRouter, Route, Routes } from "react-router-dom";
import LoginScreen from "./screens/admin/LoginScreen";
import PanelScreen from "./screens/admin/PanelScreen";

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/admin/login" element={<LoginScreen />} />
                <Route path="/admin" element={<PanelScreen />} />
            </Routes>
        </BrowserRouter>
    );
}
