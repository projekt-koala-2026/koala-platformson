import { BrowserRouter, Route, Routes } from "react-router-dom";
import TopLoadingBar from "./components/TopLoadingBar";
import { LoadingProvider } from "./contexts/LoadingContext";
import ImageHandlingScreen from "./screens/admin/ImageHandlingScreen";
import LoginScreen from "./screens/admin/LoginScreen";
import PanelScreen from "./screens/admin/PanelScreen";
import ChangePassScreen from "./screens/admin/ChangePassScreen";
import AddUserScreen from "./screens/admin/AddUserScreen";

export default function App() {
    return (
        <LoadingProvider>
            <TopLoadingBar />
            <BrowserRouter>
                <Routes>
                    <Route path="/admin/login" element={<LoginScreen />} />
                    <Route path="/admin" element={<PanelScreen />} />
                    <Route path="/admin/changepass" element={<ChangePassScreen />} />
                    <Route path="/admin/adduser" element={<AddUserScreen />} />
                    <Route path="/admin/images" element={<ImageHandlingScreen />} />
                </Routes>
            </BrowserRouter>
        </LoadingProvider>
    );
}
