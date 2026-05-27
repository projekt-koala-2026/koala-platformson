import { BrowserRouter, Route, Routes } from "react-router-dom";
import TopLoadingBar from "./components/TopLoadingBar";
import { LoadingProvider } from "./contexts/LoadingContext";
import ImageHandlingScreen from "./screens/admin/ImageHandlingScreen";
import LoginScreen from "./screens/admin/LoginScreen";
import PanelScreen from "./screens/admin/PanelScreen";
import ChangePassScreen from "./screens/admin/ChangePassScreen";
import AddUserScreen from "./screens/admin/AddUserScreen";
import EditSponsorInfo from "./screens/admin/EditSponsorInfoScreen.jsx";
import EditKoalicjantInfo from "./screens/admin/EditKoalicjantInfoScreen.jsx";
import EditRule from "./screens/admin/EditRuleScreen.jsx";
import EditPost from "./screens/admin/EditPostScreen.jsx";

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
                    <Route path="/admin/sponsors" element={<EditSponsorInfo />}/>
                    <Route path="/admin/koalicjants" element={<EditKoalicjantInfo />}/>
                    <Route path="/admin/rules" element={<EditRule />}/>
                    <Route path="/admin/posts" element={<EditPost />}/>
                </Routes>
            </BrowserRouter>
        </LoadingProvider>
    );
}
