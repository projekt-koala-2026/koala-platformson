import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import ProtectedRoute from "./components/ProtectedRoute";
import TopLoadingBar from "./components/TopLoadingBar";
import { LoadingProvider } from "./contexts/LoadingContext";
import type { Role } from "./types/models";
import { getAuthArea, getOrganizationHomePath, getUserRoles } from "./utils/authService";

const AdminLoginScreen = lazy(() => import("./screens/admin/AdminLoginScreen"));
const AdminRodoScreen = lazy(() => import("./screens/admin/AdminRodoScreen"));
const AdminTeamsScreen = lazy(() => import("./screens/admin/AdminTeamsScreen"));
const EditionsScreen = lazy(() => import("./screens/admin/EditEditionScreen"));
const SubeditionsScreen = lazy(() => import("./screens/admin/SubeditionsScreen"));
const CompetitionTasksScreen = lazy(() => import("./screens/admin/CompetitionTasksScreen"));
const RankingsScreen = lazy(() => import("./screens/admin/RankingsScreen"));
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
const CapitanTasksScreen = lazy(() => import("./screens/public/CapitanTasksScreen"));
const CompleteRegistrationScreen = lazy(
    () => import("./screens/public/CompleteRegistrationScreen")
);
const HistoryScreen = lazy(() => import("./screens/public/HistoryScreen"));
const HomeScreen = lazy(() => import("./screens/public/HomeScreen"));
const KoalicjaScreen = lazy(() => import("./screens/public/KoalicjaScreen"));
const LoginScreen = lazy(() => import("./screens/public/LoginScreen"));
const ProblemsPublicScreen = lazy(() => import("./screens/public/ProblemsPublicScreen"));
const ResetPasswordScreen = lazy(() => import("./screens/public/ResetPasswordScreen"));
const RuleScreen = lazy(() => import("./screens/public/RuleScreen"));
const PublicRankingsScreen = lazy(() => import("./screens/public/RankingsPublicScreen"));

const adminOnly: Role[] = ["ORGANIZATION_ADMIN"];
const contentEditors: Role[] = ["ORGANIZATION_ADMIN", "ORGANIZATION_EDITOR"];
const teamMembers: Role[] = ["TEAM_CAPTAIN", "TEAM_GUARDIAN"];

const RouteFallback = () => (
    <main className="flex min-h-[40vh] items-center justify-center px-4" role="status">
        <span className="text-sm font-medium text-ink-muted">Ładowanie widoku…</span>
    </main>
);

const LegacyPasswordRedirect = ({ area }: { area: "organization" | "team" }) => {
    const roles = getUserRoles();
    const matchesArea = getAuthArea(roles) === area;
    const target = matchesArea
        ? area === "organization"
            ? getOrganizationHomePath(roles)
            : "/captain"
        : area === "organization"
          ? "/admin/login"
          : "/login";
    return <Navigate to={target} replace />;
};

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
                            element={<LegacyPasswordRedirect area="organization" />}
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
                            path="/admin/subeditions"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <SubeditionsScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/competition-tasks"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <CompetitionTasksScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/admin/rankings"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <RankingsScreen />
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
                        <Route
                            path="/admin/rodos"
                            element={
                                <ProtectedRoute allowedRoles={adminOnly} redirectTo="/admin/login">
                                    <AdminRodoScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route path="/" element={<HomeScreen />} />
                        <Route path="/login" element={<LoginScreen />} />
                        <Route path="/register" element={<CompleteRegistrationScreen />} />
                        <Route path="/reset-password" element={<ResetPasswordScreen />} />
                        <Route path="/problems" element={<ProblemsPublicScreen />} />
                        <Route path="/rules" element={<RuleScreen />} />
                        <Route
                            path="/changepass"
                            element={<LegacyPasswordRedirect area="team" />}
                        />
                        <Route path="/history" element={<HistoryScreen />} />
                        <Route path="/rankings" element={<PublicRankingsScreen />} />
                        <Route path="/koalicja" element={<KoalicjaScreen />} />
                        <Route
                            path="/captain"
                            element={
                                <ProtectedRoute allowedRoles={teamMembers}>
                                    <CaptainHomeScreen />
                                </ProtectedRoute>
                            }
                        />
                        <Route
                            path="/captain/tasks"
                            element={
                                <ProtectedRoute allowedRoles={teamMembers}>
                                    <CapitanTasksScreen />
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
