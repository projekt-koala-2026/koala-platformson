import { useEffect, useMemo, useState } from "react";
import { FaEdit, FaTrash } from "react-icons/fa";
import { useNavigate } from "react-router-dom";
import AdminHeader from "../../components/AdminHeader";
import Button from "../../components/Button";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import { useLoading } from "../../contexts/LoadingContext";
import { apiRequest } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";
import styles from "./EditPostScreen.module.css";

const EditPosts = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const isAdminUser = useMemo(() => isAdmin(), []);

    const [posts, setPosts] = useState([]);
    const [postId, setPostId] = useState(null);
    const [title, setTitle] = useState("");
    const [markdownBody, setMarkdownBody] = useState("");

    const [editions, setEditions] = useState([]);
    const [editionId, setEditionId] = useState("");

    const [editingPost, setEditingPost] = useState(null);

    const savePost = async (text) => {
        startLoading();

        if (!title || title.trim() === "" || title.length < 3) {
            alert("Podaj tytuł posta, który ma ponad 2 znaki");
            stopLoading();
            return;
        }

        if (!editionId || editionId === "") {
            alert("Wybierz edycję, do której ma zostać przypisany post");
            stopLoading();
            return;
        }

        if (editingPost) {
            const apiLink = `/api/admin/post/${postId}`;
            await apiRequest(
                apiLink,
                { title: title, markdownBody: text, editionId: editionId },
                "PUT",
                navigate
            );

            setPosts((prev) =>
                prev.map((p) =>
                    p.id === postId
                        ? {
                              ...p,
                              title: title,
                              markdownBody: text,
                              editionId: editionId,
                          }
                        : p
                )
            );

            setEditingPost(null);
            setTitle("");
            setMarkdownBody("");
            setEditionId("");
            stopLoading();
            return;
        }

        const data = await apiRequest(
            "/api/admin/post",
            { title: title, markdownBody: text, editionId: editionId },
            "POST",
            navigate
        );

        await new Promise((resolve) => setTimeout(resolve, 500));

        if (data) {
            setPosts((prev) => [...prev, data]);
            navigate("/admin/posts");
        }

        setTitle("");
        setMarkdownBody("");
        setEditionId("");
        stopLoading();
    };

    const EditPost = (post) => {
        const isCurrent = editingPost === post;
        setEditingPost(isCurrent ? null : post);
        setPostId(isCurrent ? null : post.id);
        setTitle(isCurrent ? "" : post.title);
        setMarkdownBody(isCurrent ? "" : post.markdownBody);
        setEditionId(isCurrent ? "" : post.editionId || "");
    };

    const DeletePost = async (post) => {
        const confirmed = window.confirm(`Czy na pewno chcesz usunąć post ${post.title}?`);

        if (!confirmed) return;

        const apiLink = `/api/admin/post/${post.id}`;
        await apiRequest(apiLink, {}, "DELETE", navigate);
        setPosts((prev) => prev.filter((p) => p.id !== post.id));
    };

    useEffect(() => {
        if (!isAdminUser) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            startLoading();
            const postsData = await apiRequest("/api/admin/post", null, "GET", navigate);

            const editionsData = await apiRequest("/api/admin/edition", null, "GET", navigate);

            if (postsData) setPosts(postsData);
            if (editionsData) {
                setEditions(editionsData);
                if (editionsData.length > 0) {
                    setEditionId(editionsData[0].id);
                }
            }
            stopLoading();
        };

        if (isAdminUser) getData();
    }, [navigate]);

    return (
        <div className="container">
            <AdminHeader navigate={navigate} />
            {isAdminUser && (
                <>
                    <div className="container-near">
                        <div className="container">
                            {editingPost === null && (
                                <div className={styles.textOverlay}>
                                    <h1>Dodaj Post {title}</h1>
                                </div>
                            )}
                            {editingPost !== null && (
                                <div className={styles.textOverlay}>
                                    <h1>Edytuj Post {title}</h1>
                                </div>
                            )}
                            <div style={{ maxWidth: "600px", padding: "16px" }}>
                                <input
                                    type="text"
                                    placeholder="Tytuł posta"
                                    value={title}
                                    onChange={(e) => setTitle(e.target.value)}
                                    required
                                />

                                {/* Lista rozwijana wyboru edycji pobranej z bazy danych */}
                                <select
                                    value={editionId}
                                    onChange={(e) => setEditionId(e.target.value)}
                                    className={styles.editionSelect}
                                    required
                                >
                                    <option value="">-- Wybierz edycję konkursu --</option>
                                    {editions.map((e) => (
                                        <option key={e.id} value={e.id}>
                                            {e.title}
                                        </option>
                                    ))}
                                </select>

                                <MarkdownEditor
                                    key={markdownBody}
                                    initialValue={markdownBody}
                                    onChange={setMarkdownBody}
                                    onSave={(text) => {
                                        savePost(text);
                                        setEditingPost(null);
                                        setMarkdownBody("");
                                        setTitle("");
                                        setEditionId("");
                                    }}
                                />
                            </div>
                        </div>
                        <div className="container">
                            <h1>Posty z Edycji</h1>
                            <ContentsListBox>
                                {posts.map((item, idx) => {
                                    const linkedEdition = editions.find(
                                        (e) => e.id === item.editionId
                                    );

                                    return (
                                        <ContentsListTile key={item.id}>
                                            <div className={styles.postHeader}>
                                                <h3>Tytuł: {item.title}</h3>
                                                <h6>
                                                    Data:{" "}
                                                    {new Date(item.createdAt).toLocaleString(
                                                        "pl-PL",
                                                        {
                                                            year: "numeric",
                                                            month: "long",
                                                            day: "numeric",
                                                            hour: "2-digit",
                                                            minute: "2-digit",
                                                        }
                                                    )}
                                                </h6>
                                                <hr className={styles.divider} />
                                            </div>
                                            <MarkdownRenderer
                                                key={idx}
                                                content={item.markdownBody}
                                            />
                                            <hr className={styles.divider} />
                                            <h6>
                                                Edycja:{" "}
                                                {linkedEdition
                                                    ? linkedEdition.title
                                                    : "Nieprzypisana"}
                                            </h6>
                                            <div className={styles.actions}>
                                                <Button
                                                    text={<FaEdit />}
                                                    onClick={() => EditPost(item)}
                                                />
                                                <Button
                                                    text={<FaTrash />}
                                                    onClick={() => DeletePost(item)}
                                                />
                                            </div>
                                        </ContentsListTile>
                                    );
                                })}
                            </ContentsListBox>
                        </div>
                    </div>
                </>
            )}
        </div>
    );
};

export default EditPosts;
