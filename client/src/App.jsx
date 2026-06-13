import { BrowserRouter, Route, Routes } from "react-router-dom";
import TopLoadingBar from "./components/TopLoadingBar";
import { LoadingProvider } from "./contexts/LoadingContext";
import AddUserScreen from "./screens/admin/AddUserScreen";
import ChangePassScreen from "./screens/admin/ChangePassScreen";
import EditionsScreen from "./screens/admin/EditEditionScreen.jsx";
import EditHistory from "./screens/admin/EditHistoryScreen.jsx";
import EditKoalicjantInfo from "./screens/admin/EditKoalicjantInfoScreen.jsx";
import EditPost from "./screens/admin/EditPostScreen.jsx";
import EditProblemsScreen from "./screens/admin/EditProblemsScreen.jsx";
import EditRule from "./screens/admin/EditRuleScreen.jsx";
import EditSchoolsScreen from "./screens/admin/EditSchoolsScreen.jsx";
import EditSponsorInfo from "./screens/admin/EditSponsorInfoScreen.jsx";
import ImageHandlingScreen from "./screens/admin/ImageHandlingScreen";
import LoginScreen from "./screens/admin/LoginScreen";
import PanelScreen from "./screens/admin/PanelScreen";
import HistoryScreen from "./screens/public/HistoryScreen.jsx";
import HomeScreen from "./screens/public/HomeScreen";
import KoalicjaScreen from "./screens/public/KoalicjaScreen.jsx";
import ProblemsPublicScreen from "./screens/public/ProblemsPublicScreen.jsx";
import RuleScreen from "./screens/public/RuleScreen.jsx";

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
                    <Route path="/admin/problems" element={<EditProblemsScreen />} />
                    <Route path="/admin/schools" element={<EditSchoolsScreen />} />
                    <Route path="/" element={<HomeScreen />} />
                    <Route path="/problems" element={<ProblemsPublicScreen />} />
                    <Route path="/rules" element={<RuleScreen />} />
                    <Route path="/history" element={<HistoryScreen />} />
                    <Route path="/koalicja" element={<KoalicjaScreen />} />
                </Routes>
            </BrowserRouter>
        </LoadingProvider>
    );
}
