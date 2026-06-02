import { BrowserRouter, Route, Routes } from "react-router-dom";
import TopLoadingBar from "./components/TopLoadingBar";
import { LoadingProvider } from "./contexts/LoadingContext";
import AddUserScreen from "./screens/admin/AddUserScreen";
import ChangePassScreen from "./screens/admin/ChangePassScreen";
import EditionsScreen from "./screens/admin/EditEditionScreen.jsx";
import EditKoalicjantInfo from "./screens/admin/EditKoalicjantInfoScreen.jsx";
import EditPost from "./screens/admin/EditPostScreen.jsx";
import EditRule from "./screens/admin/EditRuleScreen.jsx";
import EditHistory from "./screens/admin/EditHistoryScreen.jsx";
import EditSponsorInfo from "./screens/admin/EditSponsorInfoScreen.jsx";
import ImageHandlingScreen from "./screens/admin/ImageHandlingScreen";
import LoginScreen from "./screens/admin/LoginScreen";
import PanelScreen from "./screens/admin/PanelScreen";
import HomeScreen from "./screens/public/HomeScreen";

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
                    <Route path="/admin/sponsors" element={<EditSponsorInfo />} />
                    <Route path="/admin/koalicjants" element={<EditKoalicjantInfo />} />
                    <Route path="/admin/rules" element={<EditRule />} />
                    <Route path="/admin/history" element={<EditHistory />} />
                    <Route path="/admin/posts" element={<EditPost />} />
                    <Route path="/admin/editions" element={<EditionsScreen />} />
                    <Route path="/" element={<HomeScreen />} />
                </Routes>
            </BrowserRouter>
        </LoadingProvider>
    );
}
