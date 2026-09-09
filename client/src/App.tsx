import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import ProtectedRoute from "./components/ProtectedRoute";
import TopLoadingBar from "./components/TopLoadingBar";
import { LoadingProvider } from "./contexts/LoadingContext";
import type { Role } from "./types/models";

const AdminLoginScreen = lazy(() => import("./screens/admin/AdminLoginScreen"));
const AdminTeamsScreen = lazy(() => import("./screens/admin/AdminTeamsScreen"));
const EditionsScreen = lazy(() => import("./screens/admin/EditEditionScreen"));
const EditHistory = lazy(() => import("./screens/admin/EditHistoryScreen"));
const EditKoalicjantInfo = lazy(() => import("./screens/admin/EditKoalicjantInfoScreen"));
const EditPost = lazy(() => import("./screens/admin/EditPostScreen"));
const EditProblemsScreen = lazy(() => import("./screens/admin/EditProblemsScreen"));
const EditRule = lazy(() => import("./screens/admin/EditRuleScreen"));
const EditSchoolsScreen = lazy(() => import("./screens/admin/EditSchoolsScreen"));
const EditSponsorInfo = lazy(() => import("./screens/admin/EditSponsorInfoScreen"));
const ImageHandlingScreen = lazy(() => import("./screens/admin/ImageHandlingScreen"));
const PanelScreen = lazy(() => import("./screens/admin/PanelScreen"));
const CaptainHomeScreen = lazy(() => import("./screens/public/CaptainHomeScreen"));
const HistoryScreen = lazy(() => import("./screens/public/HistoryScreen"));
const HomeScreen = lazy(() => import("./screens/public/HomeScreen"));
const KoalicjaScreen = lazy(() => import("./screens/public/KoalicjaScreen"));
const LoginScreen = lazy(() => import("./screens/public/LoginScreen"));
const ProblemsPublicScreen = lazy(() => import("./screens/public/ProblemsPublicScreen"));
const RuleScreen = lazy(() => import("./screens/public/RuleScreen"));

const adminOnly: Role[] = ["ADMIN"];
const contentEditors: Role[] = ["ADMIN", "EDITOR"];
const captainOnly: Role[] = ["CAPTAIN"];

const RouteFallback = () => (
    <main className="flex min-h-[40vh] items-center justify-center px-4" role="status">
        <span className="text-sm font-medium text-slate-500">Ładowanie widoku…</span>
    </main>
);

export default function App() {
    return (
        <LoadingProvider>
            <TopLoadingBar />
            <BrowserRouter>
                <Suspense fallback={<RouteFallback />}>
                    <Routes>
                        <Route path="/admin/login" element={<AdminLoginScreen />} />
                        <Route
                            path="/admin"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <PanelScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/changepass"
                            element={<Navigate to="/admin" replace />}
                        />
                        <Route path="/admin/adduser" element={<Navigate to="/admin" replace />} />
                        <Route
                            path="/admin/images"
                            element={
                                <ProtectedRoute
                                    allowedRoles={contentEditors}
                                    redirectTo="/admin/login"
                                >
                                    <ImageHandlingScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/sponsors"
                            element={
                                <ProtectedRoute
                                    allowedRoles={contentEditors}
                                    redirectTo="/admin/login"
                                >
                                    <EditSponsorInfo />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/koalicjants"
                            element={
                                <ProtectedRoute
                                    allowedRoles={contentEditors}
                                    redirectTo="/admin/login"
                                >
                                    <EditKoalicjantInfo />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/rules"
                            element={
                                <ProtectedRoute
                                    allowedRoles={contentEditors}
                                    redirectTo="/admin/login"
                                >
                                    <EditRule />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/history"
                            element={
                                <ProtectedRoute
                                    allowedRoles={contentEditors}
                                    redirectTo="/admin/login"
                                >
                                    <EditHistory />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/posts"
                            element={
                                <ProtectedRoute
                                    allowedRoles={contentEditors}
                                    redirectTo="/admin/login"
                                >
                                    <EditPost />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/editions"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <EditionsScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/problems"
                            element={
                                <ProtectedRoute
                                    allowedRoles={contentEditors}
                                    redirectTo="/admin/login"
                                >
                                    <EditProblemsScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/schools"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <EditSchoolsScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/teams"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <AdminTeamsScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route path="/" element={<HomeScreen />} />
                        <Route path="/login" element={<LoginScreen />} />
                        <Route path="/problems" element={<ProblemsPublicScreen />} />
                        <Route path="/rules" element={<RuleScreen />} />
                        <Route path="/changepass" element={<Navigate to="/" replace />} />
                        <Route path="/history" element={<HistoryScreen />} />
                        <Route path="/koalicja" element={<KoalicjaScreen />} />
                        <Route
                            path="/captain"
                            element={
                                <ProtectedRoute allowedRoles={captainOnly}>
                                    <CaptainHomeScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route path="*" element={<Navigate to="/" replace />} />
                    </Routes>
                </Suspense>
            </BrowserRouter>
        </LoadingProvider>
    );
}
