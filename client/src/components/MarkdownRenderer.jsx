import ReactMarkdown from "react-markdown";
import { apiUrl } from "../utils/apiFetcher";
import styles from "./MarkdownRenderer.module.css";

const MarkdownRenderer = ({ content }) => {
    const customComponents = {
        img: ({ src, alt }) => {
            const fullUrl = src.startsWith("http") ? src : `${apiUrl}${src}`;

            return <img src={fullUrl} alt={alt} className={styles.image} />;
        },
        a: ({ href, children }) => (
            <a href={href} target="_blank" rel="noopener noreferrer">
                {children}
            </a>
        ),
    };

    return (
        <div className={styles.markdownRender}>
            <ReactMarkdown components={customComponents}>{content}</ReactMarkdown>
        </div>
    );
};

export default MarkdownRenderer;
